using Cronos;

using Radzen;
using Radzen.Blazor;

namespace BackupRestore.CustomValidator;

public class CronValidator : ValidatorBase
{
    public override string Text { get; set; }

    protected override bool Validate(IRadzenFormComponent component)
    {
        var value = component.GetValue();
        var valueAsString = value as string;

        try
        {
            var expr = CronExpression.Parse(valueAsString);
        }
        catch (Exception ex)
        {

            Text = ex.Message;
            return false;
        }
        return true;
    }
}
