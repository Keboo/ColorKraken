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
    
    
    // MARK: - Properties
    var themeBuilder : ThemeBuilder? = nil
    var viewModel = ViewModel()
    
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
    
    override func viewDidLoad() {
        super.viewDidLoad()
        
        // Do any additional setup after loading the view.
        outlineView.dataSource = self
        outlineView.delegate = self
        
        self.themeBuilder = ThemeBuilder()
    }
    
    override var representedObject: Any? {
        didSet {
            // Update the view, if already loaded.
        }
    }
    
    
    // MARK: - IBAction Methods
    
    @IBAction func createCollection(_ sender: Any) {
        var collectionToExpand: Collection?
        
        if let collection = getCollectionForSelectedItem() {
            viewModel.createCollection(withTitle: "New Collection", inCollection: collection)
            collectionToExpand = collection
        } else {
            viewModel.createCollection(withTitle: "New Collection", inCollection: nil)
        }
        
        outlineView.reloadData()
        outlineView.expandItem(collectionToExpand)
    }
    
    
    @IBAction func addColor(_ sender: Any) {
        guard let collection = getCollectionForSelectedItem() else { return }
        
        let newColor = viewModel.addColor(to: collection)
        outlineView.reloadData()
        outlineView.expandItem(collection)
        
        let colorRow = outlineView.row(forItem: newColor)
        outlineView.selectRowIndexes(IndexSet(arrayLiteral: colorRow), byExtendingSelection: false)
    }
    
    
    @IBAction func removeItem(_ sender: Any) {
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
            color.update(withRed: red, green: green, blue: blue, alpha: alpha)
            outlineView.reloadItem(color)
        }
    }
}



// MARK: - NSTextFieldDelegate
extension ViewController: NSTextFieldDelegate {
    
    func control(_ control: NSControl, textShouldEndEditing fieldEditor: NSText) -> Bool {
        guard let collection = outlineView.item(atRow: outlineView.selectedRow) as? Collection else { return true }
        collection.title = (control as! NSTextField).stringValue
        return true
    }
}

// MARK: - NSOutlineview data source
extension ViewController: NSOutlineViewDataSource {
    
    func outlineView(_ outlineView: NSOutlineView, numberOfChildrenOfItem item: Any?) -> Int {
        
        //        if item == nil {
        //            return viewModel.model.totalCollections
        //        } else {
        //            if let item = item as? Collection {
        //                return item.totalItems
        //            } else {
        //                return 1
        //            }
        //        }
        return item == nil ? viewModel.model.totalCollections : (item as? Collection)?.totalItems ?? 1
    }
    
    func outlineView(_ outlineView: NSOutlineView, child index: Int, ofItem item: Any?) -> Any {
        //        if item == nil {
        //            return viewModel.model.collections[index]
        //        } else {
        //            if let collection = item as? Collection {
        //                return collection.items[index]
        //            } else {
        //                return item!
        //            }
        //        }
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
                cell.textField?.isEditable = true
                cell.textField?.delegate = self
                cell.textField?.layer?.backgroundColor = .clear
            } else if let color = item as? Color {
                
                cell.textField?.stringValue = ""
                cell.textField?.isEditable = false
                cell.textField?.wantsLayer = true
                cell.textField?.layer?.backgroundColor = color.toNSColor().cgColor
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
                cell.textField?.stringValue = color.description
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
