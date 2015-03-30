package ist.meic.pa;

import java.lang.reflect.Field;
import java.util.Stack;
/*
 * Command Info returns information about the Called Object as well as the Fields 
 * of that object and the Call Stack
 */
public class CommandInfo {
	public static void main(){

		Object lastObject = History.calledObjects.peek().getObject();
		Stack<ObjectFieldValue> copyOfCalledObjects = History.calledObjects;

		System.out.print("Called Object:	");
		System.out.println(lastObject);
		
		Field[] fields = History.calledClasses.peek().getDeclaredFields();
		String output = "";

		// Append the name of the fields to the string output
		for (int i=0; i< fields.length;i++){
			if(i==(fields.length -1)){	
				output=output.concat(fields[i].getName());
			}else{
				output=output.concat(fields[i].getName() + " ");
			}
		}

		System.out.print("       Fields:	");
		System.out.println(output);

		System.out.println("Call stack:");

		//	Clone the stacks in order to have different stacks
		Stack<ObjectFieldValue> stack = (Stack<ObjectFieldValue>) copyOfCalledObjects.clone();
		Stack<Object[]> stackArgs = (Stack<Object[]>) History.calledArgs.clone();

		//Prints all the methods and the arguments that the method has been called
		for(int i=0;i< stack.capacity(); i++){
			ObjectFieldValue currentObject = stack.pop();
			System.out.print(currentObject.className + ".");
			System.out.print(currentObject.getMethod());
			printArgs(stackArgs.pop());
		}
	}


	/*
	 * Prints the arguments
	 */
	public static void printArgs(Object[] calledArgs){
		String output="";
		System.out.print("(");
		
		// Append the name of the arguments to the string output
		for (int i=0; i< calledArgs.length;i++){
			if(i==(calledArgs.length -1)){	
				output=output.concat(calledArgs[i].toString());
			}else{
				output=output.concat(calledArgs[i].toString() + ",");
			}
		}
		System.out.println(output + ")");
	}
}
