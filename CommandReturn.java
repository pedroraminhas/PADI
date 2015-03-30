package ist.meic.pa;

import java.lang.reflect.Field;
import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;

public class CommandReturn {

	public static void main(String[] args) throws InstantiationException, IllegalAccessException, ClassNotFoundException, IllegalArgumentException, InvocationTargetException{
		
		//initialize the Map in order to have an instance of the required type
		TypeMap.initializeMap();
		
		Class lastObjectClass = History.calledObjects.peek().getObject().getClass();
		String lastMethodCalled = History.calledObjects.peek().getMethod();

		//Gets the Method Class for the last called method
		for(Method m: lastObjectClass.getDeclaredMethods()){
			if(m.getName().equals(lastMethodCalled)){
				
				//Only do for non-void methods
				if(!m.getReturnType().toString().equals("void")){
					
					
					Object obj = TypeMap.typeMap.get(m.getReturnType().toString());
					Class<?> classType = obj.getClass();
					
					//Gets valueOf with the parameter of the returned type
					for(Method m1 : classType.getDeclaredMethods()){
						if(m1.getName().equals("valueOf") && m1.getParameterTypes()[0].equals(args[1].getClass())){
							Object o = m1.invoke(classType, args[1]);
							MethodTranslator.c=o;
							
							// Make the shell stop
							DebuggerShell.canContinue=false;
						}
					}

				}else{
					// Make the shell stop
					DebuggerShell.canContinue=false;
				}

			}
		}
	}
}

