#requires -Version 5.1


class Animal {
    [string] $name = "Animal"
    Animal([string] $name) {
        $this.name = $name
    }
    [string] Speak() {
        throw "Not implemented"
    }
}