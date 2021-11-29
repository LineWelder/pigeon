### Pigeon - a simple programming language made for fun
## Features to implement
- Negation
- Explicit integer literal types
- Function stack frames and register saving
- Return statements
- Function arguments
- Local variables
- Pointers
- Strings
- Global initializations, _start and i32 main()
- Command line interface
- Dll imports
- Implement multiplication and division
- Function pointers
## Implemented features
# Basic types
Signed: i8, i16, i32
Unsigned: u8, u16, u32
Where the number is type's size in bits
# Global variables
`i16 test = 29;`
Declares a global variable named `test` of type i16 and initializes it with 29
# Functions (Work in progress!)
```
func()
{
	...
}
```
Declares a function named `func` that doesn't return any value. Analog of void functions in C.
Inside the curly braces expected statements - the code that should be executed whenever the
function is called (Function calls aren't implemented yet!)
# Veriable assignment
`test = 2 + test;`
Computes the expression on the right side and assigns the variable on the right side
to it's result
# Basic arithmetics
`test + 4 * 3 - 29`
- Available are four basic arithmetic operators: + - * /
  **Multiplication and divizion are not implemented yet!
  However if the result can be computed in compile time it will be**
- The compiler is able to optimize arithmetic expressions, so the compiled expression will be:
  `test - 17`
- The result type of an expression is the widest type of a variable used in it, or
  the type of the variable the expression result is assigned to
# Explicit type casts
- `test:i8`
  This will return the first 8 bits of test
- `test:i32`
  This will extend test to 32 bits filling the highest bits with
  - the sign bit if converting into a signed type
  - zeros if converting into an unsigned type