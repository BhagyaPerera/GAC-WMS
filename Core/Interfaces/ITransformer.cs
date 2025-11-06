namespace Core.Interfaces;

public interface ITransformer<TInput, TOutput>
{
    TOutput Transform(TInput input);
}
