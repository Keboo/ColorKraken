//
//  ThemeBuilder.swift
//  ColorKraken
//
//  Created by Bruce Gomes on 3/14/22.
//

import Foundation
import AppKit

class ThemeBuilder {
    
    let metaKey = "meta"
    let themeKey = "themeValues"
    let toolbarKey = "toolbar"
    let rootKey = "root"
    let tabsbarKey = "tabsbar"
    
    var dictData : Dictionary<String, Any>? = nil
    var metaDict : Dictionary<String, String>? = nil
    var themeValuesDict : Dictionary<String, Any>? = nil
    var toolbarDict : Dictionary<String, String>? = nil
    var rootDict : Dictionary<String, String>? = nil
    var tabsbarDict : Dictionary<String, String>? = nil
    let fileThemeBuilder = FileThemeBuilder()
    
    init() {
        
        if let dictData = fileThemeBuilder.GetFileData() {
            
            BuildThemeDict(dictData: dictData)
            print("all 3 dictionaries built succesfully")
        } else {
            print("Failed Getting Dictionary from Json")
        }
    }
    
    private func BuildThemeDict(dictData : Dictionary<String, Any>) {
        
        metaDict = dictData[metaKey] as? Dictionary<String, String>
        
        if let themeValuesDict = dictData[themeKey] as? Dictionary<String, Any> {
            
            self.dictData = dictData
            self.themeValuesDict = themeValuesDict
            
            toolbarDict = themeValuesDict[toolbarKey] as? Dictionary<String, String>
            rootDict = themeValuesDict[rootKey] as? Dictionary<String, String>
            tabsbarDict = themeValuesDict[tabsbarKey] as? Dictionary<String, String>
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
    
    func saveCurrentDictData() {
        
        themeValuesDict?.updateValue(self.toolbarDict!, forKey: self.toolbarKey)
        themeValuesDict?.updateValue(self.rootDict!, forKey: self.rootKey)
        themeValuesDict?.updateValue(self.tabsbarDict!, forKey: self.tabsbarKey)
        
        self.dictData?.updateValue(self.metaDict!, forKey: self.metaKey)
        self.dictData?.updateValue(self.themeValuesDict!, forKey: self.themeKey)
    }
    
    func saveDataToFile(withFile fileName: String) {
        
        let theJSONData = try? JSONSerialization.data(withJSONObject: self.dictData!,options: [.fragmentsAllowed, .prettyPrinted, .sortedKeys, .withoutEscapingSlashes])
        
        if theJSONData != nil, let documentDirectory = fileThemeBuilder.getGKDefaultThemePath() {
            let pathWithFileName = documentDirectory.appendingPathComponent("\(fileName).jsonc-default")
            
            do {
                try theJSONData!.write(to: pathWithFileName)
            } catch {
                print(error)
            }
        }
    }
}



