#requires -Version 7.0
using module ./Animal.psm1

class Dog: Animal {
    Dog(): base("Dog") {}
    [string] Bark() {
        return "Woof!"
    }
}