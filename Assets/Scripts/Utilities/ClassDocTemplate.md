# |CLASS_NAME|
> |Class_Description|

Base Type: |Base_Type("C# class" if object)|  
Interfaces: |Interfaces(IfAny)|  
Nested Types: |Nested_Types(IfAny)|

## Public Fields

### Field_Name
Type: `|Field_Type|`
> *|Field_Description|*
---


## Public Methods

### Method_Name(|Parameter_Types|)
**Returns:** |Return_Type| 
> *|Method_Description|*

* **Parameters**
* `|Parameter_Type|` : |Parameter_Name| = |Default_Value|  
 *|Parameter_Description|*
---

## Static Fields

## Static Methods

## Protected Fields

## Protected Methods


## Comparisons / Conversions

// conversion operators and other unary operators
### `|Source Name|` -> `|Target Name|`
> *|Method Description|*
---

// Comparison operators and other binary operators
### `|Left Name|` |Operator| `|Right Name|`
**Returns:** `|Return Type|`
> *|Method Description|*
---


//NOTES:
//Public Static fields and methods are grouped under Static Fields and Static Methods, not Public Fields and Public Methods. Private Static fields and methods are not documented.
//If there are no Interfaces, the Interfaces line should not exist, same with Nested Types.
//Everything using <see/> should include only the name of the class and not anything preceding it, and should be surrounding by `s for code formatting.
//Nested Types can be included at the bottom as long as they are very small in size.
//If a parameter does not have a default value, do not include the =.
//Any section headed by a Heading 2 that has no content should be omitted.
//If a Field is actually a Property, add *(readonly)* or *(protected readonly)* after the type based on its accessibility.
//Do not put a psudeocode plan at the beginning.
//Do not include anything denoting the parameters of Comparisons / Conversions, what the method does should be described in the method description.