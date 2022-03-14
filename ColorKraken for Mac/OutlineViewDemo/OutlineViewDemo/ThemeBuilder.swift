//
//  ThemeBuilder.swift
//  OutlineViewDemo
//
//  Created by Bruce Gomes on 3/14/22.
//  Copyright © 2022 Appcoda. All rights reserved.
//

import Foundation
import AppKit

class ThemeBuilder {
    
    let metaKey = "meta"
    let themeKey = "themeValues"
    let toolbarKey = "toolbar"
    let rootKey = "root"
    let tabsbarKey = "tabsbar"
    
    var metaDict : NSDictionary? = nil
    var toolbarDict : NSDictionary? = nil
    var rootDict : NSDictionary? = nil
    var tabsbarDict : NSDictionary? = nil
    
    init() {
        
        if let dictData = FileThemeBuilder().GetFileData() {
            BuildThemeDict(dictData: dictData)
        } else {
            print("Failed Getting Dictionary from Json")
        }
    }
    
    private func BuildThemeDict(dictData : NSDictionary) {
        
        metaDict = dictData.object(forKey: metaKey) as? NSDictionary
        
        if let themeValuesDict = dictData.object(forKey: themeKey) as? NSDictionary {
            
            toolbarDict = themeValuesDict.object(forKey: toolbarKey) as? NSDictionary
            rootDict = themeValuesDict.object(forKey: rootKey) as? NSDictionary
            tabsbarDict = themeValuesDict.object(forKey: tabsbarKey) as? NSDictionary
        } else {
            print("Failed Getting Themecomponents from dictioary")
        }
    }
    
    func GetTotalElements() -> Int {
        
        var total = 0
        
        for dict in [self.toolbarDict, self.tabsbarDict, self.rootDict] {
            if dict != nil {
                total = dict!.count
            }
        }
        
        return total
    }
}



