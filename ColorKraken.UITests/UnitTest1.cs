using System.Threading.Tasks;

using XamlTest;

using Xunit;

namespace ColorKraken.UITests
{
    public class UnitTest1
    {
        [Fact]
        public async Task LoadExistingProfiles()
        {
            await using IApp app = XamlTest.App.StartRemote<App>();
            var window = app.GetMainWindow();
            await Task.Delay(10_000);
        }
    }
}