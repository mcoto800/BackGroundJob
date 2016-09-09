using SelectPdf;
using System;
using System.Data;
using System.IO;
using System.Net.Mail;

namespace BackGroundJob
{
    public class Generator
    {
        wsTicket.TicketReservation_WSSoapClient wsTickets = new wsTicket.TicketReservation_WSSoapClient();
        public Generator()
        {
        }
        #region MainMethods
        public void verifyClientFlights()
        {
            DataSet flights = wsTickets.getFlightsDepartingHours(48);
            Console.WriteLine(DateTime.Now.ToString() + ": Traveler Flights found: " + flights.Tables[0].Rows.Count.ToString());
            foreach (DataRow flight in flights.Tables[0].Rows)
            {
                sendCheckReminder(flight);
            }
        }

        public void verifyFlightsForStaff()
        {
            DataSet flights = wsTickets.getFlightsDepartingHours(1);
            string htmlReportForPDF = "";
            int airlineID = -1;
            DataSet laststaff=null;
            DataRow lastflight = null;
            Console.WriteLine(DateTime.Now.ToString() + ": Staff Flights found: "+flights.Tables[0].Rows.Count.ToString()); 
            foreach (DataRow flight in flights.Tables[0].Rows)
            {
                DataSet staff = null;
                lastflight = flight;
                if (int.Parse(flight["AIRLINE_ID"].ToString()) != airlineID)
                {
                    if (htmlReportForPDF != "")
                    {
                        htmlReportForPDF += getReportTableFooter();
                        sendFlightReport(flight, staff, htmlReportForPDF);

                    }
                    airlineID = int.Parse(flight["AIRLINE_ID"].ToString());
                    staff = wsTickets.getStaffofAirline(airlineID);
                    laststaff = staff;
                    htmlReportForPDF = getReporHeader();

                }
                htmlReportForPDF += getReportOneRow(flight);

            }
            if (htmlReportForPDF != "" && lastflight!=null && laststaff.Tables.Count>0)
            {
                Console.WriteLine(DateTime.Now.ToString() + ": Sending report");
                htmlReportForPDF += getReportTableFooter();
                sendFlightReport(lastflight, laststaff, htmlReportForPDF);
            }
            
        }
        #endregion
        #region Flight Report
        public string getReportOneRow(DataRow flight)
        {


            string html = @"<tr style='color:#000066;'>
			<td><a href='" + Properties.Settings.Default.appLocation + "/ManageTrip.aspx?rsvn=" + flight["RESERVATION_NUMER"] + @"' target='_blank' style='color:#000066;' >View</a>
            </td><td>&nbsp;</td>
            <td>" + flight["Flight Number"] + @"</td>
            <td>" + flight["Departure_Date"].ToString() + @"</td>
            <td>" + flight["Estimated hours"] + @"</td>
            <td>" + flight["From"] + @"</td>
            <td>" + flight["To"] + @"</td>
            <td>" + flight["Airline"] + @"</td>
            <td>" + flight["AirPlane"] + @"</td>
            <td>" + flight["Client"] + @"</td>
		</tr>";

            return html;
        }

        public string getReporHeader()
        {
            string html = @"<table cellspacing='0' cellpadding='3' rules='all' id='gvResults' style='background-color:White;border-color:#CCCCCC;border-width:1px;border-style:None;width:100%;border-collapse:collapse;'>
		                    <tbody><tr style='color:White;background-color:#006699;font-weight:bold;'>
			                <th scope='col'>&nbsp;</th><th scope='col'><a href='javascript:__doPostBack('gvResults','Sort$Seat Number')' style='color:White;'>Seat Number</a></th><th scope='col'><a href='javascript:__doPostBack('gvResults','Sort$Flight Number')' style='color:White;'>Flight Number</a></th><th scope='col'><a href='javascript:__doPostBack('gvResults','Sort$Departure')' style='color:White;'>Departure</a></th><th scope='col'><a href='javascript:__doPostBack('gvResults','Sort$Estimated hours')' style='color:White;'>Estimated hours</a></th><th scope='col'><a href='javascript:__doPostBack('gvResults','Sort$From')' style='color:White;'>From</a></th><th scope='col'><a href='javascript:__doPostBack('gvResults','Sort$To')' style='color:White;'>To</a></th><th scope='col'><a href='javascript:__doPostBack('gvResults','Sort$Airline')' style='color:White;'>Airline</a></th><th scope='col'><a href='javascript:__doPostBack('gvResults','Sort$Airplane')' style='color:White;'>Airplane</a></th><th scope='col'><a href='javascript:__doPostBack('gvResults','Sort$Client')' style='color:White;'>Traveler</a></th></tr>";
            return html;
        }

        public string getReportTableFooter()
        {

            return "</tbody></table>";
        }

        public bool sendFlightReport(DataRow flight, DataSet staff, string reportHTML)
        {
            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");

                mail.From = new MailAddress(Properties.Settings.Default.smtpUser);
                foreach (DataRow staffUser in staff.Tables[0].Rows)
                {
                    mail.To.Add(staffUser["EMAIL"].ToString());
                }

                mail.Subject = "Flight report " + flight["Airline"].ToString();
                mail.Body = emailFlightReportTemplateHTML(flight)+reportHTML;
                mail.IsBodyHtml = true;

                //NOT WORKING ON WINDOWS AZURE
              /*  Console.WriteLine(DateTime.Now.ToString() + ": Create attachment");
                Attachment att = new Attachment(createPDFReport(reportHTML, flight["Airline"].ToString()));
                mail.Attachments.Add(att);
                */

                SmtpServer.Port = 587;
                SmtpServer.Credentials = new System.Net.NetworkCredential(Properties.Settings.Default.smtpUser, Properties.Settings.Default.smtpPwd);
                SmtpServer.EnableSsl = true;
                
                SmtpServer.Send(mail);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private string createHTMLFileReport(string htmlReport, string airline)
        {
            string myTempFile = Path.Combine(Path.GetTempPath(), "temp" + airline + "Flights.html");
            using (StreamWriter sw = new StreamWriter(myTempFile))
            {
                sw.WriteLine(htmlReport);
            }
            return myTempFile;
        }

        private string createPDFReport(string htmlReport, string airline)
        {

            string htmlFileDir = createHTMLFileReport(htmlReport, airline);
            // create a new pdf document
            PdfDocument doc = new PdfDocument();

            // add a new page to the document
            PdfPage page = doc.AddPage();


            // create html element 
            PdfHtmlElement html = new PdfHtmlElement(htmlFileDir);

            // add the html element to the document
            page.Add(html);

            // save pdf document
            string myTempFile = Path.Combine(Path.GetTempPath(), airline + "Flights.pdf");
            //Stream stream=new MemoryStream();
            doc.Save(myTempFile);

            // close pdf document
            doc.Close();
            return myTempFile;

        }

        private string emailFlightReportTemplateHTML(DataRow flight)
        {



            string html = @"<div style='width:900px;padding:20px 0;margin-top:10px'>
<table cellspacing='0' cellpadding='0' style='border:1px solid #007;border-bottom:0;border-top:0;padding:10px;padding-right:9px;font-family:Verdana,Arial;font-size:10px;color:#000000;width:744px' summary='Layout table for text content'>
<tbody><tr><td>
  
  <table border='0' cellspacing='0' cellpadding='0' style='width:100%;border:1px solid #0000b6'>
    <tbody><tr>
      <td style='padding:10px;font-size:10px;font-family:Verdana,Arial'> <p> The attached file contains the flights departing in one hour. Click the link to <a href='" + Properties.Settings.Default.appLocation + "/StaffManageFlights.aspx?airline=" + flight["AIRLINE_ID"] + @"' target='_blank' >MANAGE THIS FLIGHTS</a> </p>
        
        <p>The option to cancel the flight or one of the passangers is available in the flight manager.</p>
        </td></tr>
    </tbody></table><br>
  
  </div>";


            return html;

        }
        #endregion

        #region CheckIn Reminder
        private string emailCheckInTemplateHTML(DataRow flight)
        {



            string html = @"<div style='width:900px;padding:20px 0;margin-top:10px'>
<table cellspacing='0' cellpadding='0' style='border:1px solid #007;border-bottom:0;border-top:0;padding:10px;padding-right:9px;font-family:Verdana,Arial;font-size:10px;color:#000000;width:744px' summary='Layout table for text content'>
<tbody><tr><td>
  <table border='0' cellspacing='0' cellpadding='0' style='width:100%'><tbody><tr>
    <td style='width:16px'><img src='https://ci6.googleusercontent.com/proxy/qn1nISMOZ8Y1i_nlH5Np_nYB7_ypAVm2faoIHeGaBdYzGZ6X5jmHfXxvcDLBrVU4dPxNJ61QtO8X6HGFzLQCqhhznI0-auIs=s0-d-e1-ft#http://www.ryanair.com/emails/images/BYF/top1l.gif' alt='leftcorner' width='16' height='24' class='CToWUd'></td>
    <td style='font-family:Verdana,Arial;background-color:#0000b6;color:#ffffff;font-size:14px;font-weight:bold'>
      YOUR <span class='il'>RESERVATION</span> <span class='il'></span>
      " + flight["RESERVATION_NUMER"] + @" DEPARTS IN 48 Hours</td>
    <td style='width:16px'><img src='https://ci4.googleusercontent.com/proxy/BXlRzPNsq2tSAjGxiJsGFzU-_D4u02TSZNt5MP1zc9Llm5XCtZbmh7KlvXQdlSME8NFR1_f0XJ1zLzQDNXMDmydMSWHvLMUp=s0-d-e1-ft#http://www.ryanair.com/emails/images/BYF/top1r.gif' alt='rightcorner' width='16' height='24' class='CToWUd'></td>
    </tr></tbody></table><br>
  
  <table border='0' cellspacing='0' cellpadding='0' style='width:100%;border:1px solid #0000b6'>
    <tbody><tr>
      <td style='padding:10px;font-size:10px;font-family:Verdana,Arial'> <p> REMEBER YOU MUST <a href='" + Properties.Settings.Default.appLocation + "/ManageTrip.aspx?rsvn=" + flight["RESERVATION_NUMER"] + @"' target='_blank' >CHECK-IN ONLINE</a> AND PRINT YOUR BOARDING PASS ON AN INDIVIDUAL A4 PAGE FOR PRESENTATION AT BOTH AIRPORT SECURITY AND AT THE BOARDING GATE. </p>
        <p>You can check-in online up to 4 hours before each scheduled flight departure time. </p>
        <p>The option to reserve a seat is available on all our flights</p>
        </td></tr>
    </tbody></table><br>
  
  </div>";


            return html;

        }

        public bool sendCheckReminder(DataRow flight)
        {
            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");

                mail.From = new MailAddress(Properties.Settings.Default.smtpUser);
                mail.To.Add(flight["EMAIL"].ToString());
                mail.Subject = "Remeber check-in for your trip " + flight["RESERVATION_NUMER"];
                mail.Body = emailCheckInTemplateHTML(flight);
                mail.IsBodyHtml = true;

                SmtpServer.Port = 587;
                SmtpServer.Credentials = new System.Net.NetworkCredential(Properties.Settings.Default.smtpUser, Properties.Settings.Default.smtpPwd);
                SmtpServer.EnableSsl = true;

                SmtpServer.Send(mail);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        #endregion
    }
}