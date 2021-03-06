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
    var toolbarDict : Dictionary<String, String> = [:]
    var rootDict : Dictionary<String, String> = [:]
    var tabsbarDict : Dictionary<String, String> = [:]
    
    let fileThemeBuilder = FileThemeBuilder()
    
    init(withPicker picker : NSComboBox) {
        
        fileThemeBuilder.configurePicker(picker: picker)
        buildData()
        
    }
    
    func buildData(forceDefault : Bool = false) {
        
        if let dictData = fileThemeBuilder.GetFileData(forUrl: nil, forceDefaultData: forceDefault) {
            
            BuildThemeDict(dictData: dictData)
            print("all 3 dictionaries built succesfully")
        } else {
            print("Failed Getting Dictionary from GK default Json")
        }
    }
    
    func setDataForSelectedItem(selectedItem : Any?) -> Bool {
        
        if let themeUrl = selectedItem as? URL, let dictData = fileThemeBuilder.GetFileData(forUrl: themeUrl) {
            BuildThemeDict(dictData: dictData)
            return true
        }
        
        return false
    }
    
    private func BuildThemeDict(dictData : Dictionary<String, Any>) {
        
        metaDict = dictData[metaKey] as? Dictionary<String, String>
        
        if let themeValuesDict = dictData[themeKey] as? Dictionary<String, Any> {
            
            self.dictData = dictData
            self.themeValuesDict = themeValuesDict
            
            toolbarDict = themeValuesDict[toolbarKey] as! Dictionary<String, String>
            rootDict = themeValuesDict[rootKey] as! Dictionary<String, String>
            tabsbarDict = themeValuesDict[tabsbarKey] as! Dictionary<String, String>
        } else {
            print("Failed Getting Theme components from dictioary")
        }
    }
    
    func GetTotalElements() -> Int {
        
        var total = 0
        
        for dict in [self.toolbarDict, self.tabsbarDict, self.rootDict] {
            
            total += dict.count
        }
        
        return total
    }
    
    func saveCurrentDictData() {
        
        themeValuesDict?.updateValue(self.toolbarDict, forKey: self.toolbarKey)
        themeValuesDict?.updateValue(self.rootDict, forKey: self.rootKey)
        themeValuesDict?.updateValue(self.tabsbarDict, forKey: self.tabsbarKey)
        
        self.dictData?.updateValue(self.metaDict!, forKey: self.metaKey)
        self.dictData?.updateValue(self.themeValuesDict!, forKey: self.themeKey)
    }
    
    func saveDataToFile(withFile fileName: String, newFile : Bool = true) {
        
        let theJSONData = try? JSONSerialization.data(withJSONObject: self.dictData!,options: [.fragmentsAllowed, .prettyPrinted, .sortedKeys, .withoutEscapingSlashes])
        
        var pathWithFileName : URL? = nil
        
        if newFile, let documentDirectory = fileThemeBuilder.getGKDefaultThemePath() {
            pathWithFileName = documentDirectory.appendingPathComponent("\(fileName).jsonc")
        } else {
            pathWithFileName = URL.init(string: fileName)
        }
        
        if theJSONData != nil && pathWithFileName != nil {
            do {
                try theJSONData!.write(to: pathWithFileName!)                
            } catch {
                print(error)
            }
        }
    }
    
    func updateValue(forColor color: Color, forDictionaryType type: ColorType){
        
        let value = color.colorWheelMode ? color.rgbaDescription : color.valueName
        
        switch type {
            
        case .root:
            self.rootDict.updateValue(value, forKey: color.keyName)
            
        case .tabsbar:
            self.tabsbarDict.updateValue(value, forKey: color.keyName)
            
        case .toolbar:
            self.toolbarDict.updateValue(value, forKey: color.keyName)
            
        default:
            print("dictionary type: \(type) could not be updated")
        }
    }
}



