# Pigeon - a simple programming language made for fun
## Features to implement
- Local variables
- Global initializations, _start and i32 main()
- Command line interface
- Branching and loops
- Pointers
- Arrays
- Strings
- Multiplication and division
- Function pointers
## Implemented features
### Basic number types
- Signed: `i8`, `i16`, `i32`
- Unsigned: `u8`, `u16`, `u32`<br/>
Where the number is type's size in bits.
### Global variables
```
i16 test = 29;
```
Declares a global variable named `test` of type `i16` and initializes it with `29`.
### Functions
```
func()
{
    ...
}
```
Declares a function named `func` that doesn't return any value. Analog of void functions from C.
Inside the curly braces expected statements - the code that should be executed whenever the
function is called.
Inside such a function `return;` statement can be used to immediately end the function
execution.
#### Returning values from functions
```
i16 test_add_2()
{
    return test + 2;
}
```
The return statement ends current function execution. The returned value becomes the value
of the function call, so `test_add_2()` will be equal to `31`.
#### Taking values from outside
```
i16 add(i16 a, i16 b)
{
    return a + b;
}
```
A function can take multiple values to operate on, they can be accessed
withing the function body just as normal variables.<br/>
**If there is a variable with the same name as an argument, it will
be unaccessable from within the function body since all the mentions of it
will be interpreted as the argument mentions.**
#### Calling functions
```
func();
test = 4 + test_add_2();
test = sum(2, 3);
```
Functions can be call either in an expression or in a separate statement. However, only
functions that return a value can be called within an expression.<br/>
To assign values to the function arguments they should be simply listed in the parentheses.
### Variable assignment
```
test = 2 + test;
```
Computes the expression on the right side and assigns the variable on the left side
to it's result.
### Basic arithmetics
```
-test + 4 * 3 - 29
```
- Available are the four basic arithmetic operators: `+` `-` `*` `/`<br/>
  And the unary `-` to negate values. `test * -2` and `test + -2` are valid expressions<br/>
  **Multiplication and divizion are not implemented yet!
  However if the result can be computed at compile time it will be**
- The compiler is able to optimize arithmetic expressions, so the compiled expression will be
  `-17 - test`
- The result type of an expression is the widest type of a variable used in it, or
  the type of the variable the expression result is assigned to
### Explicit type casts
- `test:i8` - the first 8 bits of `test`
- `test:i32` - `test` extended to 32 bits by filling the highest bits with
  - the sign bit if converting into a signed type
  - zeros if converting into an unsigned type
