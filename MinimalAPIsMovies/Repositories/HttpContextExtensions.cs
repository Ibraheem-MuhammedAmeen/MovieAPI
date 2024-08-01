using Microsoft.EntityFrameworkCore;

namespace MinimalAPIsMovies.Repositories
{
    //httpcontext class allows us go get httprequest and make modifications to it
    //IQueryable allows us to make operation to a table without specifiying the table we are talking about
    public static class HttpContextExtensions
    {
        public async static Task InsertPaginationParameterInResponseHeader<T>(
            this HttpContext httpContext, IQueryable<T> queryable)
        {
            if (httpContext is null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            double count = await queryable.CountAsync();
            httpContext.Response.Headers.Append("totalAmountOfRecords", count.ToString());
        }
    }
}
