//
//  FileThemeBuilder.swift
//  OutlineViewDemo
//
//  Created by Bruce Gomes on 3/13/22.
//  Copyright © 2022 Appcoda. All rights reserved.
//

import Foundation
import AppKit

class FileThemeBuilder {
    
    func GetFileData() -> NSDictionary? {
        
        var fileData : NSDictionary? = nil
        
        if let fileURL = GetThemeFileUrl() {
            
            let dataStr = try? String(contentsOf: fileURL)
            
            do {
                fileData = try JSONSerialization.jsonObject(with: (dataStr!.data(using: .utf8))!, options:  [.json5Allowed, .fragmentsAllowed]) as? NSDictionary 
                
                print("valid")
            } catch {
                print(error)
            }
        } else {
            print("Invalid File Url")
        }
        
        return fileData
    }
    
    func GetThemeFileUrl() -> URL? {
        
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
