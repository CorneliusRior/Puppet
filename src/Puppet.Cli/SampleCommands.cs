using System;
using System.Collections.Generic;
using System.Text;

namespace Puppet.Cli
{
    internal class SampleCommands : IPuppetCommandSet
    {
        public IReadOnlyList<PuppetCommand> Commands =>
        [
            new PuppetCommand(
                name: "Assessment",                
                executeAsync: ConfirmAsync,
                description: "Asks for your assessment on a number of topics."               
            )
        ];

        private async Task ConfirmAsync(PuppetContext  context, IReadOnlyList<string> args, CancellationToken cancellationToken)
        {
            bool pizza = await context.ConfirmRequireAsync("Do you like pizza?", "Sorry, I didn't hear that.");
            bool hotDogs = await context.ConfirmRequireAsync("Do you like hotdogs?", "I can't understand you.");
            bool dinosaurs = await context.ConfirmRequireAsync("Do you like dinosaurs", "Too ambiguous, do you or do you not like dinosaurs?");
            double maxDeadLift = await context.RequireDoubleAsync("What is your max deadlift?", "Don't be shy, DYEL?");

            context.WriteLine("Here is your assessment of various things:");
            context.WriteLine(pizza ? "You like pizza :)" : "You don't like pizza :(");
            context.WriteLine(hotDogs ? "You like hotdogs :)" : "You don't like hotdogs :(");
            context.WriteLine(dinosaurs ? "You like dinosaurs :)" : "You don't like dunosaurs :(");
            context.WriteLine($"Your max deadlift is {maxDeadLift}Kg, {(maxDeadLift < 100 ? "DYEL?" : maxDeadLift < 200 ? "Natty" : "Not natty")}");
            return;
        }
    }
}
