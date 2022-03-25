//
//  Model.swift
//  ColorKraken
//
//  Created by Bruce Gomes
//

import AppKit

/**
 General Notes:
 
 * Color class represents a color in the app, and Collection
 represents a collection of colors.
 * They are classes and not structs because we want their instances
 to pass around by reference and changes made to an object to affect
 the original one, not a copy of it.
 * Both conform to Equatable protocol for comparing objects. To make
 comparison easy they contain the "id" property as identifiers.

 */


/**
 It represents a color object in app.
 */
class Color: Equatable, CustomStringConvertible {
    var id: Int?
    var red: CGFloat = 0.0
    var green: CGFloat = 0.0
    var blue: CGFloat = 0.0
    var alpha: CGFloat = 1.0
    var keyName : String = ""
    var valueName : String = ""
    var colorWheelMode = false
    
    /// It returns the RGBA values formatted as they should be
    /// displayed to the outline view.
    var description: String {
        return "\(String(format: "R: %.3f", red)), \(String(format: "G: %.3f", green)), \(String(format: "B: %.3f", blue)), \(String(format: "A: %.2f", alpha))"
    }
    
    var rgbaDescription: String {
        return "rgba(\(String(format: "%.f", red * 255)),\(String(format: "%.f", green * 255)),\(String(format: "%.f", blue * 255)),\(String(format: "%1.f", alpha)))"
    }
    
    init(withID id: Int) {
        self.id = id
    }
    
    
    /**
     It returns a NSColor object based on the RGBA values
     of the current object.
    */
    func toNSColor() -> NSColor {
        return NSColor(red: red, green: green, blue: blue, alpha: alpha)
    }

    
    /**
     Update color object using the RGBA values given as arguments.
    */
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



/**
 It represents a collection of colors and other collections.
 */
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
        // Check if the given item is a collection.
        // In that case remove all of its items.
        if T.self == Collection.self {
            // The given item is a Collection so remove all of its items.
            (item as! Collection).items.removeAll()
        }
        
        // Find the given item in the items array and remove it.
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
 It handles the top level collections and the identifiers
 of the Color and Collection classes. It's also what the
 View Model uses as its model.
*/
struct Model {
    var collections = [Collection]()
    var totalCollections: Int { get { return collections.count }}
    private var nextCollectionID = 1
    private var nextColorID = 1
    
    /**
     It returns the current Collection ID and increases
     it by 1 to the next value.
    */
    mutating func getCollectionID() -> Int {
        nextCollectionID += 1
        return nextCollectionID - 1
    }
    
    /**
     It returns the current Color ID and increases
     it by 1 to the next value.
    */
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
