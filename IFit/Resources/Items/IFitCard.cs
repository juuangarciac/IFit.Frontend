using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace IFit.Resources.Items
{
    public class IFitCard
    {
        public string? ImageSource { get; set; }
        public string? Title { get; set; }
        public string? TextBody { get; set; }
        public string? TextButtonAccept { get; set; }
        public string? TextButtonDecline { get; set; }

        public IFitCard(string? imageSource, string? title, string? textBody, string? textButtonAccept, string? textButtonDecline)
        {
            ImageSource = imageSource;
            Title = title;
            TextBody = textBody;
            TextButtonAccept = textButtonAccept;
            TextButtonDecline = textButtonDecline;
        }
    }
}
