using Microsoft.SemanticKernel;

namespace LocalPlugin;


public class AutoInvocationFilter : IAutoFunctionInvocationFilter
{
    public async Task OnAutoFunctionInvocationAsync(AutoFunctionInvocationContext context, Func<AutoFunctionInvocationContext, Task> next)
    {
        try
        {
            Console.WriteLine($"Agent Start Call Function");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Function: {context.Function.Name} called");
            if (context.Arguments != null)
            {
                for (int i = 0; i < context.Arguments.Count; i++)
                {
                    Console.WriteLine($"- Arg: {context.Arguments.ToList()[i]}");
                }
            }
            Console.WriteLine($"Agent End Call Function");


            await next(context);
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error:" + e.Message);
            throw;
        }
        finally
        {
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}