**Custom serializer implementation:**

Please provide the following:

1. Provide the ```IListSerializer``` interface implementation 
(you can use any serialization format but you could not utilize third-party libraries that will serialize the full list for you. I.e. it's allowed to utilize third-party libraries for serializing 1 node in particular format):
- provided data structures, class names or namespaces could not be changed;
- solution is allowed to be not thread safe;
- it's guaranteed that list provided as an argument to ```Serialize``` and ```DeepCopy``` function is consistent and doesn't contain any cycles;
- automated testing of your solution will be performed, the resulting rate for the solution will be given based on (in order of priority):
  - tests on correctness of the solution 
  - performance tests 
  - tests on memory consumption
  
2. Write your own test cases for the implementation for ```IListSerializer``` interface.