//
//  FileThemeBuilder.swift
//  ColorKraken
//
//  Created by Bruce Gomes on 3/13/22
//

import Foundation
import AppKit

class FileThemeBuilder {
    
    let fileManager = FileManager.default
    var fileThemePicker: NSComboBox? = nil
    
    func GetFileData() -> Dictionary<String, Any>? {
        
        var fileData : Dictionary<String, Any>? = nil
        
        if let fileURL = GetCustomThemesURL() ?? GetDefaultThemeFileUrl() {
            
            let dataStr = try? String(contentsOf: fileURL)
            
            do {
                fileData = try JSONSerialization.jsonObject(with: (dataStr!.data(using: .utf8))!, options:  [.json5Allowed, .fragmentsAllowed]) as? Dictionary<String, Any>
                
                print("valid")
            } catch {
                print(error)
            }
        } else {
            print("Invalid File Url")
        }
        
        return fileData
    }
    
    func GetCustomThemesURL() -> URL? {
        let filePath = getGKDefaultThemePath()
        let customThemeExtension = ".jsonc"
        var customThemes : [URL] = []
        
        do {
            let items = try fileManager.contentsOfDirectory(atPath: filePath!.path)
            let urls = items.filter({$0.containsExtension(word: customThemeExtension)})
            
            for file in urls {
                //customThemes = [URL]()
                customThemes.append((getGKDefaultThemePath()?.appendingPathComponent(file))!)
            }
            
        } catch {
            // failed to read directory – bad permissions, perhaps?
            // TODO:  show this alert to the user
            print("Custom File URLs Not Found, or Directory doesn't have permissions")
        }
        
        if !customThemes.isEmpty {
            populatePickerWithCustomThemes(customThemes: customThemes)
            return customThemes.first
        }
        
        return nil
    }
    
    func configurePicker(picker : NSComboBox) {
        
        self.fileThemePicker = picker
        self.fileThemePicker?.removeAllItems()
        self.fileThemePicker?.isEditable = false
        self.fileThemePicker?.isEnabled = true
        self.fileThemePicker?.hasVerticalScroller = true
    }
    
    func populatePickerWithCustomThemes(customThemes : [URL]) {
        
        self.fileThemePicker?.addItems(withObjectValues: customThemes)
        self.fileThemePicker?.selectItem(withObjectValue: customThemes.first)
    }
    
    func GetDefaultThemeFileUrl() -> URL? {
        
        var filePath = getGKDefaultThemePath()
        let defaultFileExtension = ".jsonc-default"
        var fileName = isDarkMode() ? "dark" : "light"
        fileName += defaultFileExtension
        
        do {
            let items = try fileManager.contentsOfDirectory(atPath: filePath!.path)
            
            var found = false
            for item in items {
                
                if fileName.compare(item, options: .caseInsensitive) == .orderedSame {
                    filePath!.appendPathComponent(fileName)
                    found = true
                    print("Found \(item)")
                }
            }
            if !found, let file = items.first(where: { $0.contains(defaultFileExtension)}) {
                filePath!.appendPathComponent(file)
            }
        } catch {
            // failed to read directory – bad permissions, perhaps?
            // TODO:  show this alert to the user
            print("File Not Found, or Directory doesn't have permissions")
        }
        
        return filePath
    }
    
    func getGKDefaultThemePath() -> URL? {
        
        var defaultGKThemePath = fileManager.homeDirectoryForCurrentUser
        defaultGKThemePath.appendPathComponent(".gitkraken/themes")
        
        return defaultGKThemePath
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

extension String {
    func containsExtension(word : String) -> Bool
    {
        return self.range(of: "\(word)$", options: .regularExpression) != nil
    }
}
