//
//  Model.swift
//  ColorKraken
//
//  Created by Bruce Gomes
//

import AppKit

/** This is a class and not struct because we want instances
 to pass around by reference, so changes made to an object affects
 the original instead of a copy.
 */

/// Represents a color object in app.
class Color: Equatable, CustomStringConvertible {
    var id: Int?
    var red: CGFloat = 0.0
    var green: CGFloat = 0.0
    var blue: CGFloat = 0.0
    var alpha: CGFloat = 1.0
    var keyName : String = ""
    var valueName : String = ""
    var colorWheelMode = false
    
    /// - Returns: the RGBA values formatted displayed in the outline view.
    var description: String {
        return "\(String(format: "R: %.3f", red)), \(String(format: "G: %.3f", green)), \(String(format: "B: %.3f", blue)), \(String(format: "A: %.2f", alpha))"
    }
    
    var rgbaDescription: String {
        return "rgba(\(String(format: "%.f", red * 255)),\(String(format: "%.f", green * 255)),\(String(format: "%.f", blue * 255)),\(String(format: "%1.f", alpha)))"
    }
    
    init(withID id: Int) {
        self.id = id
    }
    
    /// - Returns: NSColor object based on the RGBA values of current object.
    func toNSColor() -> NSColor {
        return NSColor(red: red, green: green, blue: blue, alpha: alpha)
    }
    
    /// Update color object using the RGBA values given as arguments.
    ///  - Parameter RGBA color Compoenents: components of a color red, green,blue, and aplha as floats
    func update(withRed red: CGFloat, green: CGFloat, blue: CGFloat, alpha: CGFloat) {
        self.red = red
        self.green = green
        self.blue = blue
        self.alpha = alpha
    }
    
    static func == (lhs: Color, rhs: Color) -> Bool {
        return lhs.id == rhs.id
    }
}


/// represents a collection of colors and other collections.
class Collection: Equatable {
    var id: Int?
    var title: String?
    var items = [Any]()
    var totalItems: Int { get { return items.count }}
    var colorType : ColorType = .none
    
    init(withTitle title: String, id: Int) {
        self.title = title
        self.id = id
    }
    
    /**
     It removes either a color or another collection from
     the current collection.
     This is a generic method as it accepts two different
     data types (Color, Collection).
     */
    func remove<T>(item: T) {
        // Check if the given item is a collection and remove all of its items.
        if T.self == Collection.self {
            // Item is Collection, so remove all of its items.
            (item as! Collection).items.removeAll()
        }
        
        // Find the item in the array and remove it.
        for (index, currentItem) in items.enumerated() {
            guard type(of: currentItem) == T.self, currentItem as? T.Type == item as? T.Type else { continue }
            items.remove(at: index)
            break
        }
    }
    
    
    static func == (lhs: Collection, rhs: Collection) -> Bool {
        return lhs.id == rhs.id
    }
}

/**
 Handles the top level collections and the identifiers of the Color and Collection classes.
 The View Model uses this as its model.
 */
struct Model {
    var collections = [Collection]()
    var totalCollections: Int { get { return collections.count }}
    private var nextCollectionID = 1
    private var nextColorID = 1
    
    /// - Returns: current Collection ID increasing it by 1
    mutating func getCollectionID() -> Int {
        nextCollectionID += 1
        return nextCollectionID - 1
    }
    
    /// - Returns: current Color ID and increases it by 1
    mutating func getColorID() -> Int {
        nextColorID += 1
        return nextColorID - 1
    }
}

enum ColorType {
    
    case root
    case toolbar
    case tabsbar
    case none
}
