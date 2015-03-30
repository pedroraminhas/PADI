package ist.meic.pa;

import java.lang.reflect.Field;
import java.util.Scanner;
import java.util.Stack;
/*
 * DebuggerShell invokes the method according to the command given
 */
public class DebuggerShell {
	public static boolean canContinue = true;
	public static void main(Exception e) throws Throwable{
		while(canContinue){

			String[] input;
			Scanner in = new Scanner(System.in);
			System.out.print("DebuggerCLI:> ");
			input = in.nextLine().split(" ");

			switch (input[0]){

			case "Info":
				CommandInfo.main();
				break;

			case "Get":
				CommandGet.main(input);
				break;

			case "Abort":
				CommandAbort.main();
				break;
				
			case "Set":
				CommandSet.main(input);
				break;
				
			case "Retry":
				CommandRetry.main();
				break;
			
			case "Return":
				CommandReturn.main(input);
				break;
				
			case "Throw":
				History.calledObjects.pop();
				History.calledArgs.pop();
				throw e.getCause();
			}
		}
		canContinue=true;
	}		
}
