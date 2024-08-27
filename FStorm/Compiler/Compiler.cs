namespace FStorm
{


    public abstract class Compiler<T>
    {
        protected readonly FStormService fStormService;

        public Compiler(FStormService fStormService)
        {
            this.fStormService = fStormService;
        }

        public abstract CompilerContext<T> Compile(CompilerContext<T> context);
    }



    


}
