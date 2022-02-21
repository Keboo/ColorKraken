//
//  ThemeBuilder.swift
//  ColorKraken
//
//  Created by Bruce Gomes on 2/15/22.
//

import Foundation
import AppKit

class ThemeBuilder {
    
    init() {
        if let data = FileThemeBuilder().GetFileData() {
            let themeDict = try? JSONDecoder().decode(Theme.self, from: data)
            print(themeDict!)
        }
    }
}

private struct Theme : Codable {
    var meta : Meta
    var themeValues : ThemeValues
    
    struct Meta: Codable {
        var name : String
        var scheme : String
    }
    
    struct ThemeValues: Codable {
        
        var root : root
        var toolbar : toolbar
        var tabsbar : tabsbar
        
        struct root : Codable {
            var rootValues : [String:String]
        }
        
        struct toolbar : Codable {
            var toolbarValues : [String:String]
        }
        
        struct tabsbar : Codable {
            var tabsbarValues : [String:String]
        }
    }
}


