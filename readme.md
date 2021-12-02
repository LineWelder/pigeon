# Pigeon - a simple programming language made for fun
## Features to implement
- Function stack frames and register saving
- Return statements and function calls
- Function arguments
- Local variables
- Branching and loops
- Pointers
- Strings
- Global initializations, _start and i32 main()
- Command line interface
- Dll imports
- Implement multiplication and division
- Function pointers
## Implemented features
### Basic types
- Signed: `i8`, `i16`, `i32`
- Unsigned: `u8`, `u16`, `u32`<br/>
Where the number is type's size in bits
### Global variables
```
i16 test = 29;
```
Declares a global variable named `test` of type `i16` and initializes it with `29`
### Functions (Work in progress!)
```
func()
{
    ...
}
```
Declares a function named `func` that doesn't return any value. Analog of void functions in C.
Inside the curly braces expected statements - the code that should be executed whenever the
function is called (Function calls aren't implemented yet!)
### Variable assignment
```
test = 2 + test;
```
Computes the expression on the right side and assigns the variable on the left side
to it's result
### Basic arithmetics
```
-test + 4 * 3 - 29
```
- Available are four basic arithmetic operators: + - * /<br/>
  And unary - to negate values. `test * -2` and `test + -2` are valid expressions<br/>
  **Multiplication and divizion are not implemented yet!
  However if the result can be computed at compile time it will be**
- The compiler is able to optimize arithmetic expressions, so the compiled expression will be
  `-17 - test`
- The result type of an expression is the widest type of a variable used in it, or
  the type of the variable the expression result is assigned to
### Explicit type casts
- `test:i8` - the first 8 bits of test
- `test:i32` - test extended to 32 bits by filling the highest bits with
  - the sign bit if converting into a signed type
  - zeros if converting into an unsigned type
