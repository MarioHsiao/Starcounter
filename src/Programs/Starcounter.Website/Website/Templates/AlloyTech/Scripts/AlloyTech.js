function preventDoubleClick(button) {
    if (typeof(Page_ClientValidate) == 'function') {
        if (Page_ClientValidate() == false) { return false;}
    }
    if (button.processing) { button.disabled = true; }
    else { button.processing = true; }    
}
