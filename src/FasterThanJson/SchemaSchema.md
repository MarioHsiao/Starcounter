


Object Id="SchemaSchema"
   0:String Id="Id"
   1:Char Id="Type"
   2:String Id="Reuse"
   3:Object Id="Metadata"
      0:String Id="Cargo"
      1:Array Id="Patterns"
	     0:Object Id="Pattern"
		    0:String Id="Id"
   4:Array Id="Children"
      0:Object Reuse="SchemaSchema"


TypedArrays are stored as tuples with the first
value being an unsinged integer and the following 
values being the tuples of the array.

UntypedArrays are stored as tuples with the first
value being an unsinged integer and following values being objects
with two values. The first value is an integer
containing the TID of the referred template and the
second value is the tuple corresponding to that
template.

