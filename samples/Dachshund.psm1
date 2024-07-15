#requires -Version 7.0
using module ./Dog.psm1

class Dachshund: Dog {

    Dachshund() {
        $this.name = "Dachshund ($($this.Name))"
    }
    
    [string] Speak() {
        return "Woof!"
    }
}
