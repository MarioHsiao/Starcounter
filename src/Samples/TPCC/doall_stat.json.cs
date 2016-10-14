using Starcounter;

partial class doall_stat : Json 
{
    static doall_stat() 
    {
        DefaultTemplate.ElapsedTime.InstanceType = typeof(double);
        DefaultTemplate.TransactionsPerSecond.InstanceType = typeof(double);
    }
}
