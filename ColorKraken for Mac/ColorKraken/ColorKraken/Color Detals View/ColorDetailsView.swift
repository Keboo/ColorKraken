//
//  ColorDetailsView.swift
//  ColorKraken
//
//  Created by Bruce Gomes
//

import Cocoa


protocol ColorDetailsViewDelegate {
    func shouldUpdateColor(withRed red: CGFloat, green: CGFloat, blue: CGFloat, alpha: CGFloat)
}


class ColorDetailsView: NSView {
    
    // MARK: - IBOutlet Properties
    
    @IBOutlet weak var colorWell: NSColorWell!

    @IBOutlet weak var redLabel: NSTextField!
    
    @IBOutlet weak var greenLabel: NSTextField!
    
    @IBOutlet weak var blueLabel: NSTextField!
    
    @IBOutlet weak var alphaLabel: NSTextField!
    
    
    // MARK: - Properties
    
    var delegate: ColorDetailsViewDelegate?
    
    var color: Color?
    
    
    // MARK: - Init
    
    init() {
        super.init(frame: NSRect.zero)
        loadFromNib()
    }
    
    
    required init?(coder aDecoder: NSCoder) {
        super.init(coder: aDecoder)
    }
    
    
    // MARK: - IBAction Methods
    
    @IBAction func showColorsPanel(_ sender: Any) {
        redLabel.stringValue = String(format: "Red: %.f", colorWell.color.redComponent * 255)
        greenLabel.stringValue = String(format: "Green: %.f", colorWell.color.greenComponent * 255)
        blueLabel.stringValue = String(format: "Blue: %.f", colorWell.color.blueComponent * 255)
        alphaLabel.stringValue = String(format: "Alpha: %.2f", colorWell.color.alphaComponent)
        
        delegate?.shouldUpdateColor(withRed: colorWell.color.redComponent,
                                    green: colorWell.color.greenComponent,
                                    blue: colorWell.color.blueComponent,
                                    alpha: colorWell.color.alphaComponent)        
    }
    
    
    
    // MARK: - Custom Methods
    
    func show() {
        self.isHidden = false
    }
    
    func hide() {
        self.isHidden = true
    }
    
    func set(color: Color) {
        colorWell.color = color.toNSColor()
        redLabel.stringValue = String(format: "Red: %.f", color.red * 255)
        greenLabel.stringValue = String(format: "Green: %.f", color.green * 255)
        blueLabel.stringValue = String(format: "Blue: %.f", color.blue * 255)
        alphaLabel.stringValue = String(format: "Alpha: %.f", color.alpha)
    }
 
    
    
    // MARK: - Fileprivate Methods
    
    func loadFromNib() {
        var nibObjects: NSArray?
        let nibName = NSNib.Name(stringLiteral: "ColorDetailsView")
        
        if Bundle.main.loadNibNamed(nibName, owner: self, topLevelObjects: &nibObjects) {
            guard let nibObjects = nibObjects else { return }
            
            let viewObjects = nibObjects.filter { $0 is NSView }
            
            if viewObjects.count > 0 {
                guard let view = viewObjects[0] as? NSView else { return }
                self.addSubview(view)
                
                view.translatesAutoresizingMaskIntoConstraints = false
                view.leadingAnchor.constraint(equalTo: self.leadingAnchor).isActive = true
                view.trailingAnchor.constraint(equalTo: self.trailingAnchor).isActive = true
                view.topAnchor.constraint(equalTo: self.topAnchor).isActive = true
                view.bottomAnchor.constraint(equalTo: self.bottomAnchor).isActive = true
            }
        }
    }
}
