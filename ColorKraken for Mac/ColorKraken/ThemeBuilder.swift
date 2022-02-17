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
        if let data = GetDefaultTheme() {
            let themeDict = try? JSONDecoder().decode(Theme.self, from: data)
            print(themeDict!)
        }
    }
    
    private func GetDefaultTheme() -> Data? {
        let assetName = isDarkMode() ? "dark" : "light"
        let dataAsset = NSDataAsset(name: assetName, bundle: Bundle.main)?.data
       
        return dataAsset
    }
    
    private func isDarkMode() -> Bool {
        let mode = NSAppearance.currentDrawing().name
        if mode == .aqua {
            return false
        } else {
            return true
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


