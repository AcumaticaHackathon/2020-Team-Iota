using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.SM;
using PX.Objects.CS;
using PX.Objects.CR;
using PX.Objects.SM;
using PX.Objects.EP;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using HtmlAgilityPack;

namespace LuisAutoMailer
{
    public class AnotherServiceRegistration : Autofac.Module
    {
        protected override void Load(Autofac.ContainerBuilder builder)
        {
            PX.Data.DependencyInjection.ApplicationStartActivation
                .ActivateOnApplicationStart<MailProcessor>(builder, PX.Objects.EP.EmailProcessorManager.Register);
        }
    }


    public class MailProcessor : BasicEmailProcessor
    {
        public MailProcessor()
        {
            PXTrace.WriteInformation("Sales Order mail Processor instantiated. ");
        }

        protected override bool Process(Package package)
        {
            PXTrace.WriteInformation("Processing email package. ");

            var account = package.Account;
            var accountExt = account.GetExtension<PX.SM.EMailAccountExt>();

            //Checkin if Incoming Processing is active & SO Processing
            if (account.IncomingProcessing != true ||
                accountExt.UsrCustomMailer != true)
            {
                PXTrace.WriteInformation("IncomingProcessing: " + account.IncomingProcessing + ", SOProcessing: " + accountExt.UsrCustomMailer + ". ");
                return false;
            }

            var message = package.Message;

            //Check if not empty email
            if (!string.IsNullOrEmpty(message.Exception)
                || message.IsIncome != true
                || message.RefNoteID != null)
            {

                PXTrace.WriteInformation("IsIncome: " + message.IsIncome + ", RefNoteID: " + message.RefNoteID + ". ");
                return false;
            }

            string mailBody = HtmlRemoval.StripTagsRegexCompiled(package.Message.Body);
            LuisAPI predictionEngine = PXGraph.CreateInstance<LuisAPI>();
            var myTask = predictionEngine.PostPrediction (mailBody);
            string result;

            try
            {
                result = myTask.Result;
            }
            catch (Exception ex)
            {
                throw new PXException(ex.Message);
            }

            //Create BuildersPO Graph

            //Get File Attachment
            UploadFile file = SelectFrom<UploadFile>.
                              InnerJoin<NoteDoc>.On<UploadFile.fileID.IsEqual<NoteDoc.fileID>>.SingleTableOnly.
                              Where<UploadFile.noteID.IsEqual<@P.AsGuid>>.View.Select(package.Graph, message.NoteID);
            
            MailerSOGraph soGraph = PXGraph.CreateInstance<MailerSOGraph>();
            soGraph.GenerateSalesOrders(result, package.Message.MailFrom, Convert.ToDateTime(package.Message.CreatedDateTime));

            return true;
        }
    }
}