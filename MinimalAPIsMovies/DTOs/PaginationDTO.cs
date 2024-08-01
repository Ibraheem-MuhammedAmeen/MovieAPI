namespace MinimalAPIsMovies.DTOs
{
    public class PaginationDTO
    {
        public int Page { get; set; } = 1;
        private int recordsPerPage = 10;
        private readonly int recordsPerPageMax = 50;


        public int RecordsPerPage
        {
            get
            {
                return recordsPerPage;
            }
            set
            {
                //if the among of recode the user want to show per page is gretter than 50 it should show 50 else show that value
                if(value > recordsPerPageMax)
                {
                    recordsPerPage = recordsPerPageMax;
                }
                else
                {
                    recordsPerPage = value;
                }
            }
        }
    }
}
