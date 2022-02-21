//
//  FileThemeBuilder.swift
//  ColorKraken
//
//  Created by Bruce Gomes on 2/19/22.
//

import Foundation
import AppKit

class FileThemeBuilder {
        
    func GetFileData() -> Data? {
        
        var fileData : Data? = nil
        
        if let fileURL = GetThemeFileUrl() {
            fileData = try? Data(contentsOf: fileURL, options: .mappedIfSafe)
        }
        
        return fileData
    }
    
    private func GetThemeFileUrl() -> URL? {
        
        let fileManager = FileManager.default
        var filePath = fileManager.homeDirectoryForCurrentUser
        let defaultFileExtension = ".jsonc-default"
        var fileName = isDarkMode() ? "dark" : "light"
        fileName += defaultFileExtension
        
        filePath.appendPathComponent(".gitkraken/themes")
        do {
            let items = try fileManager.contentsOfDirectory(atPath: filePath.path)
            
            var found = false
            for item in items {
                
                if fileName.compare(item, options: .caseInsensitive) == .orderedSame {
                    filePath.appendPathComponent(fileName)
                    found = true
                    print("Found \(item)")
                }
            }
            if !found, let file = items.first(where: { $0.contains(defaultFileExtension)}) {
                filePath.appendPathComponent(file)
            }
        } catch {
            // failed to read directory – bad permissions, perhaps?
            // TODO:  show this alert to the user
            print("File Not Found, or Directory doesn't have permissions")
        }
        
        return filePath
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
