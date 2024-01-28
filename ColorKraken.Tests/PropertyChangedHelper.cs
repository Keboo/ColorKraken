using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ColorKraken.Tests;

/// <summary>
/// Based on https://intellitect.com/blog/making-unit-testing-easier/
/// </summary>
public static class PropertyChangedHelper
{
    public static IPropertyChanges<T> WatchPropertyChanges<T>(this INotifyPropertyChanged propertyChanged, string propertyName)
    {
        if (propertyChanged == null) throw new ArgumentNullException(nameof(propertyChanged));
        if (propertyName == null) throw new ArgumentNullException(nameof(propertyName));

        return new PropertyChangedEnumerable<T>(propertyChanged, propertyName);
    }

    private class PropertyChangedEnumerable<T> : IPropertyChanges<T>
    {
        private readonly List<T> _values = [];
        private readonly Func<T> _getPropertyValue;
        private readonly string _propertyName;
        private readonly List<(Func<T, bool>, TaskCompletionSource)> _waitHandles = [];

        public PropertyChangedEnumerable(INotifyPropertyChanged propertyChanged, string propertyName)
        {
            _propertyName = propertyName;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.Public;
            var propertyInfo = propertyChanged.GetType().GetProperty(propertyName, flags) 
                ?? throw new ArgumentException($"Could not find public property getter for {propertyName} on {propertyChanged.GetType().FullName}");
            var instance = Expression.Constant(propertyChanged);
            var propertyExpression = Expression.Property(instance, propertyInfo);
            _getPropertyValue = Expression.Lambda<Func<T>>(propertyExpression).Compile();

            propertyChanged.PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.Equals(_propertyName, e.PropertyName, StringComparison.Ordinal))
            {
                var value = _getPropertyValue();
                _values.Add(value);
                _waitHandles.ForEach(t =>
                    {
                        if (t.Item1(value)) t.Item2.SetResult();
                    });
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Task WaitForChange(CancellationToken token)
        {
            return WaitFor(x => true, token);
        }

        public Task WaitFor(Func<T, bool> predicate, CancellationToken token)
        {
            TaskCompletionSource tcs = new();
            _waitHandles.Add((predicate, tcs));
            return tcs.Task;
        }
    }
}

public interface IPropertyChanges<out T> : IEnumerable<T>
{
    Task WaitForChange(CancellationToken token = default);
    Task WaitFor(Func<T, bool> predicate, CancellationToken token = default);
}