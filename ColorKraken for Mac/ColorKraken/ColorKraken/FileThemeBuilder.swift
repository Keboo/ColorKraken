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
    @IBOutlet weak var fileThemePicker: NSComboBox!
    
    func GetFileData() -> Dictionary<String, Any>? {
        
        var fileData : Dictionary<String, Any>? = nil
        
        if let fileURL = GetDefaultThemeFileUrl() {
            
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
