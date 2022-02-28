//
//  ViewController.swift
//  ColorKraken
//
//  Created by Bruce Gomes on 2/6/22.
//

import Cocoa

class ViewController: NSViewController, NSTableViewDelegate, NSTableViewDataSource {
    
    @IBOutlet weak var tableView: NSTableView!
    var themeBuilder : ThemeBuilder? = nil
    
    
    fileprivate enum CellIdentifiers {
        static let ColorNameCell = "colorNameCellID"
        static let ColorCodeCell = "colorCodeCellID"
        static let ColorWheelCell = "colorWheelCellID"
    }
    
    override func viewDidLoad() {
        super.viewDidLoad()
        
        configureTable()
        // Do any additional setup after loading the view.
        self.themeBuilder = ThemeBuilder()
    }
    
    override var representedObject: Any? {
        didSet {
            // Update the view, if already loaded.
        }
    }
    
    func configureTable() {
        
        self.tableView.dataSource = self
        self.tableView.delegate = self
        self.tableView.tableColumns[0].headerCell.title = "Theme Type"
        self.tableView.tableColumns[1].headerCell.title = "Color Code"
        self.tableView.tableColumns[2].headerCell.title = "Color Wheel"
        self.tableView.gridStyleMask = .solidHorizontalGridLineMask
    }
    
    func numberOfRows(in tableView: NSTableView) -> Int {
        
        // number of lines in json file
        return self.themeBuilder?.GetTotalElements() ?? 0
    }
    
    func tableView(_ tableView: NSTableView, heightOfRow row: Int) -> CGFloat {
        40
    }     
    
    func tableView(_ tableView: NSTableView, viewFor tableColumn: NSTableColumn?, row: Int) -> NSView? {
        
        var cellIndentifier: String = ""
        var cellText = "Testing"
        
        if tableColumn == tableView.tableColumns[0] {
            
            cellIndentifier = CellIdentifiers.ColorNameCell
            cellText = "ColorType"
            
        } else if tableColumn == tableView.tableColumns[1] {
            
            cellIndentifier = CellIdentifiers.ColorCodeCell
            cellText = "#ffffff"
            
        } else if tableColumn == tableView.tableColumns[2] {
            
            cellIndentifier = CellIdentifiers.ColorWheelCell
            cellText = "ColorWheel"
        }
        
        if let cell = tableView.makeView(withIdentifier: NSUserInterfaceItemIdentifier(rawValue: cellIndentifier), owner: nil) as? NSTableCellView {
            cell.textField?.stringValue = cellText
            return cell
        }
        
        return nil
    }
}

