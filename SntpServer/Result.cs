using System;

namespace SntpServer
{
	public class Result<T>
	{
		public readonly string ErrorMessage;
		public readonly T Value;

		public bool IsError => ErrorMessage != null;

		public Result(T value, string errorMessage = null)
		{
			ErrorMessage = errorMessage;
			Value = value;
		}
	}

	public static class Result
	{
		public static Result<T> Of<T>(Func<T> f, string error = null)
		{
			try
			{
				return Ok(f());
			}
			catch (Exception e)
			{
				return Fail<T>(error ?? e.Message);
			}
		}

		public static Result<T> Fail<T>(string errorMessage)
		{
			return new Result<T>(default(T), errorMessage);
		}

		public static Result<T> Ok<T>(T value)
		{
			return new Result<T>(value);
		}

		public static Result<TOutput> Then<TInput, TOutput>(
			this Result<TInput> input,
			Func<TInput, TOutput> continuation)
		{
			if (input.IsError)
				return Fail<TOutput>(input.ErrorMessage);

			return Of(() => continuation(input.Value));
		}

		public static Result<TOutput> Then<TInput, TOutput>(
			this Result<TInput> input,
			Func<TInput, Result<TOutput>> continuation)
		{
			if (input.IsError)
				return Fail<TOutput>(input.ErrorMessage);

			return continuation(input.Value);
		}

		public static Result<TInput> OnFail<TInput>(
			this Result<TInput> input,
			Action<string> handleError)
		{
			if (input.IsError)
				handleError(input.ErrorMessage);
			return input;
		}

		public static Result<TInput> OnSuccess<TInput>(
			this Result<TInput> input,
			Action<TInput> action)
		{
			if (!input.IsError)
				action(input.Value);
			return input;
		}
	}
}
