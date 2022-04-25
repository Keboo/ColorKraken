//
//  ViewController.swift
//  ColorKraken
//
//  Created by Bruce Gomes
//

import Cocoa

class ViewController: NSViewController {
    
    // MARK: - IBOutlet Properties
    
    @IBOutlet weak var outlineView: NSOutlineView!
    @IBOutlet weak var containerView: NSView!
    @IBOutlet weak var fileThemePicker: NSComboBox!
    var themeBuilder : ThemeBuilder? = nil
    var viewModel = ViewModel()
    var curThemeName : String? = nil
    
    lazy var colorDetailsView: ColorDetailsView = {
        let view = ColorDetailsView()
        view.delegate = self
        view.isHidden = true
        self.containerView.addSubview(view)
        view.translatesAutoresizingMaskIntoConstraints = false
        view.centerXAnchor.constraint(equalTo: self.containerView.centerXAnchor).isActive = true
        view.centerYAnchor.constraint(equalTo: self.containerView.centerYAnchor).isActive = true
        view.widthAnchor.constraint(equalTo: self.containerView.widthAnchor).isActive = true
        view.heightAnchor.constraint(equalToConstant: 400.0).isActive = true
        return view
    }()
    
    // MARK: - VC Lifecycle
    
    required init?(coder: NSCoder) {
        super.init(coder: coder)
    }
    
    override func viewDidLoad() {
        super.viewDidLoad()
        
        self.themeBuilder = ThemeBuilder(forPicker: fileThemePicker)
        outlineView.dataSource = self
        outlineView.delegate = self
        
        createDefaultTheme()
    }
    
    func createDefaultTheme() {
        
        if let themeBuilder = themeBuilder {
            loadDict(withTitle: "Root Itens", dictionary: themeBuilder.rootDict, colorType: .root)
            loadDict(withTitle: "ToolBar Itens", dictionary: themeBuilder.toolbarDict, colorType: .toolbar)
            loadDict(withTitle: "Tabsbar Itens", dictionary: themeBuilder.tabsbarDict, colorType: .tabsbar)
        }
    }
    
    // MARK: - Dictionary/File manipulation
    func loadDict(withTitle title: String, dictionary: Dictionary<String, String>, colorType: ColorType) {
        
        if dictionary.count != 0 {
            
            let collection = viewModel.createCollection(withTitle: title, inCollection: nil)
            collection.colorType = colorType
            
            loadDictItems(inCollection: collection, fromDict: dictionary)
            
            outlineView.reloadData()
            outlineView.expandItem(collection)
        } else {
            print("couldn't load dictionary from themeBuilder property")
        }
    }
    
    func loadDictItems(inCollection collection: Collection, fromDict dict: Dictionary<String, String>) {
        
        for (key,val) in dict {
            
            let color = viewModel.addColor(to: collection)
            color.keyName = key
            color.valueName = val
        }
    }
    
    override var representedObject: Any? {
        didSet {
            // Update the view, if already loaded.
        }
    }
    
    // MARK: - IBAction Methods
    
    @IBAction func selectTheme(_ sender: Any) {
        
         print("Selected......")
    }
    
    @IBAction func createTheme(_ sender: Any) {
        
        let alertController = NSAlert()
        alertController.messageText = "Enter The New Theme Name"
        alertController.alertStyle = .informational
        alertController.addButton(withTitle: "Cancel")
        alertController.addButton(withTitle: "Save")
        
        let textBox = NSTextField(string: "my theme name")
        alertController.accessoryView = textBox
        textBox.frame.size = CGSize(width: alertController.window.frame.size.width - 20, height: 20)
        
        if alertController.runModal() == .alertSecondButtonReturn {
            self.curThemeName = textBox.stringValue
            print("creating file: \(self.curThemeName ?? "not found")")
        }
    }
    
    @IBAction func saveTheme(_ sender: Any) {
        
        if self.curThemeName == nil || self.curThemeName == "" {
            
            createTheme((Any).self)
            
        } else if let themeBuilder = self.themeBuilder {
            
            themeBuilder.metaDict?.updateValue(self.curThemeName ?? "defaultName", forKey: "name")
            themeBuilder.saveCurrentDictData()
            themeBuilder.saveDataToFile(withFile: self.curThemeName ?? "defaultName")
        }
    }
    
    @IBAction func removeItem(_ sender: Any) {
        
        // TODO: this might come in handy if it turns out GK accepts partial json and user wants delete items they dont care about
        if false {
            let selectedRow = outlineView.selectedRow
            var result = false
            
            if let selectedItem = outlineView.item(atRow: outlineView.selectedRow) as? Color, let parentCollection = getCollectionForSelectedItem() {
                viewModel.remove(item: selectedItem, from: parentCollection)
                result = true
            } else if let selectedItem = outlineView.item(atRow: outlineView.selectedRow) as? Collection {
                if let parentCollection = outlineView.parent(forItem: selectedItem) as? Collection {
                    viewModel.remove(item: selectedItem, from: parentCollection)
                } else {
                    viewModel.remove(item: selectedItem, from: nil)
                }
                
                result = true
            }
            
            if result {
                outlineView.reloadData()
                
                if selectedRow < outlineView.numberOfRows {
                    outlineView.selectRowIndexes(IndexSet(arrayLiteral: selectedRow), byExtendingSelection: false)
                } else {
                    if selectedRow - 1 > -1 {
                        outlineView.selectRowIndexes(IndexSet(arrayLiteral: selectedRow - 1), byExtendingSelection: false)
                    }
                }
            }
        }
    }
    
    func getCollectionForSelectedItem() -> Collection? {
        let selectedItem = outlineView.item(atRow: outlineView.selectedRow)
        
        guard let selectedCollection = selectedItem as? Collection else {
            return outlineView.parent(forItem: selectedItem) as? Collection
        }
        return selectedCollection
    }
}



// MARK: - ColorDetailsViewDelegate
extension ViewController: ColorDetailsViewDelegate {
    func shouldUpdateColor(withRed red: CGFloat, green: CGFloat, blue: CGFloat, alpha: CGFloat) {
        
        if let color = outlineView.item(atRow: outlineView.selectedRow) as? Color {
            
            color.colorWheelMode = true
            color.update(withRed: red, green: green, blue: blue, alpha: alpha)
            let colorType = getCollectionForSelectedItem()?.colorType ?? ColorType.none
            self.themeBuilder?.updateValue(forColor: color, forDictionaryType: colorType)
            outlineView.reloadItem(color)
        }
    }
}



// MARK: - NSTextFieldDelegate
extension ViewController: NSTextFieldDelegate {
    
    // TODO: Change this to fill values field and save value to dict item
    func control(_ control: NSControl, textShouldEndEditing fieldEditor: NSText) -> Bool {
        //        guard let collection = outlineView.item(atRow: outlineView.selectedRow) as? Collection else { return true }
        //        collection.title = (control as! NSTextField).stringValue
        guard let color = outlineView.item(atRow: outlineView.selectedRow) as? Color else { return true }
        let textFieldValue = (control as! NSTextField).stringValue
        color.valueName = textFieldValue
        let colorType = getCollectionForSelectedItem()?.colorType ?? ColorType.none
        color.colorWheelMode = false
        self.themeBuilder?.updateValue(forColor: color, forDictionaryType: colorType)
        
        return true
    }
}

// MARK: - NSOutlineview data source
extension ViewController: NSOutlineViewDataSource {
    
    func outlineView(_ outlineView: NSOutlineView, numberOfChildrenOfItem item: Any?) -> Int {
        
        return item == nil ? viewModel.model.totalCollections : (item as? Collection)?.totalItems ?? 1
    }
    
    func outlineView(_ outlineView: NSOutlineView, child index: Int, ofItem item: Any?) -> Any {
        
        return item == nil ? viewModel.model.collections[index] : (item as? Collection)?.items[index] ?? item!
    }
    
    func outlineView(_ outlineView: NSOutlineView, isItemExpandable item: Any) -> Bool {
        guard let _ = item as? Collection else { return false }
        
        return true
    }
}

// MARK: - NSOutlineview Delegate
extension ViewController: NSOutlineViewDelegate {
    
    func outlineView(_ outlineView: NSOutlineView, viewFor tableColumn: NSTableColumn?, item: Any) -> NSView? {
        
        guard let colIdentifier = tableColumn?.identifier else { return nil }
        if colIdentifier == NSUserInterfaceItemIdentifier(rawValue: "col1") {
            
            let cellIdentifier = NSUserInterfaceItemIdentifier(rawValue: "cell1")
            guard let cell = outlineView.makeView(withIdentifier: cellIdentifier, owner: nil) as? NSTableCellView else { return nil }
            
            if let collection = item as? Collection {
                cell.textField?.stringValue = collection.title ?? ""
                cell.textField?.isEditable = false
                cell.textField?.layer?.backgroundColor = .clear
            } else if let color = item as? Color {
                
                cell.textField?.stringValue = color.keyName
                cell.textField?.isEditable = false
                cell.textField?.wantsLayer = true
                cell.textField?.layer?.backgroundColor = .clear
                cell.textField?.layer?.cornerRadius = 5.0
            }
            return cell
            
        } else {
            let cellIdentifier = NSUserInterfaceItemIdentifier(rawValue: "cell2")
            guard let cell = outlineView.makeView(withIdentifier: cellIdentifier, owner: nil) as? NSTableCellView else { return nil }
            
            if let collection = item as? Collection {
                cell.textField?.stringValue = collection.totalItems != 1 ? "\(collection.totalItems) items" : "1 item"
                cell.textField?.font = NSFont.boldSystemFont(ofSize: cell.textField?.font?.pointSize ?? 13.0)
            } else if let color = item as? Color {
                if color.colorWheelMode {
                    cell.textField?.stringValue = color.rgbaDescription
                    color.colorWheelMode = false
                } else {
                    cell.textField?.stringValue = color.valueName
                }
                cell.textField?.isEditable = true
                cell.textField?.delegate = self
                cell.textField?.font = NSFont.systemFont(ofSize: cell.textField?.font?.pointSize ?? 13.0)
            }
            return cell
        }
    }
    
    func outlineViewSelectionDidChange(_ notification: Notification) {
        
        if let color = outlineView.item(atRow: outlineView.selectedRow) as? Color {
            colorDetailsView.set(color: color)
            colorDetailsView.show()
        } else {
            colorDetailsView.hide()
        }
    }
}
