#requires -Version 7.0
using module ./Animal.psm1

class Cat: Animal {
    Cat(): base("Cat") {
    }
    
    [string] Speak() {
        return "Meow!"
    }
}