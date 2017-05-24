using Bukimedia.PrestaSharp.Factories;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Rproducer_new_customers
{
    class Program
    {
        private static string BASE_URL = "http://10.3.51.37:32200/api";
        private static string ACCOUNT = "HAK8IHRW4KWXTQS8CAIJ7YESQDUNYCPX";
        private static string PASSWORD = "";
        class Message
        {
            public string firstname { get; set; }
            public string lastname { get; set; }
            public string email { get; set; }
            public string birthday { get; set; }
            public string address1 { get; set; }
            public string address2 { get; set; }
            public string postcode { get; set; }
            public string city { get; set; }
            public string phone { get; set; }
            public string phone_mobile { get; set; }
        }
        static void Main(string[] args)
        {
            List<Bukimedia.PrestaSharp.Entities.customer> actual_list = new List<Bukimedia.PrestaSharp.Entities.customer>();
            
            //NOTIFY NEW CUSTOMERS
            while (true)
            {
                //Get all users from HMS
                CustomerFactory customers = new CustomerFactory(BASE_URL, ACCOUNT, PASSWORD);
                List<Bukimedia.PrestaSharp.Entities.customer> result_list = customers.GetAll();

                //CHECK FOR NEW DATA
                //Check
                IEnumerable<Bukimedia.PrestaSharp.Entities.customer> tmp = new List<Bukimedia.PrestaSharp.Entities.customer>();
                if (actual_list.Count() == 0)
                    tmp = result_list;
                else
                {
                    //tmp = result_list.Where(
                    //    item => actual_list.Any(i => !i.id.Equals(item.id))).ToList();

                    foreach (Bukimedia.PrestaSharp.Entities.customer c in result_list)
                    {
                        bool contain = false;
                        foreach (Bukimedia.PrestaSharp.Entities.customer c1 in actual_list)
                        {
                            if (c.id == c1.id)
                            {
                                contain = true;
                                break;
                            }
                        }
                        if (contain == false)
                        {
                            List<Bukimedia.PrestaSharp.Entities.customer> tmp1 = tmp.ToList<Bukimedia.PrestaSharp.Entities.customer>();
                            tmp1.Add(c);
                            tmp = tmp1.ToList();
                        }

                    }
                }

                //Pull messages from data
                Message[] messages = new Message[tmp.Count()];
                if (tmp.Count() != 0 || actual_list.Count() == 0)
                {

                    int index = 0;
                    foreach (Bukimedia.PrestaSharp.Entities.customer c in tmp)
                    {

                        //Message
                        Bukimedia.PrestaSharp.Entities.address customer_address = getAdres(c.id);
                        Message message = new Message()
                        {
                            firstname = c.firstname,
                            lastname = c.lastname,
                            email = c.email,
                            birthday = c.birthday,
                            address1 = (customer_address.address1 != null) ? customer_address.address1.ToString() : "",
                            address2 = (customer_address.address2 != null) ? customer_address.address2.ToString() : "",
                            postcode = (customer_address.postcode != null) ? customer_address.postcode.ToString() : "",
                            city = (customer_address.city != null) ? customer_address.city.ToString() : "",
                            phone = (customer_address.phone != null) ? customer_address.phone.ToString() : "",
                            phone_mobile = (customer_address.phone_mobile != null) ? customer_address.phone_mobile.ToString() : "",
                        };
                        messages[index] = message;
                        index++;

                    }
                    
                }
                
                //SEND NEW DATA
                if (messages.Count() != 0)
                {
                    //RabbitMq
                    ConnectionFactory factory = new ConnectionFactory();
                    factory.UserName = "guest";
                    factory.Password = "guest";
                    factory.HostName = "10.3.51.37";

                    IConnection conn = factory.CreateConnection();
                    IModel channel = conn.CreateModel();

                    channel.ExchangeDeclare(
                        exchange:"new_customer_exchange", 
                        type: ExchangeType.Direct);
                    channel.QueueDeclare(
                        queue: "new_customer_queue", 
                        durable: true, 
                        exclusive: false, 
                        autoDelete: false, 
                        arguments: null);
                    channel.QueueBind(
                        queue: "new_customer_queue", 
                        exchange: "new_customer_exchange", 
                        routingKey: "new_customer_queue");

                    string messageBody = JsonConvert.SerializeObject(messages);
                    byte[] messageBodyBytes = Encoding.UTF8.GetBytes(messageBody);

                    //Send
                    channel.BasicPublish(
                        exchange: "new_customer_exchange", 
                        routingKey: "new_customer_queue", 
                        basicProperties: null, 
                        body: messageBodyBytes);

                    Console.WriteLine("new data sended.");
                    channel.Dispose();
                    conn.Dispose();
                }
                
                actual_list = result_list;

            }
        }
        
        public static Bukimedia.PrestaSharp.Entities.address getAdres(long? id)
        {
            AddressFactory addresses = new AddressFactory(BASE_URL, ACCOUNT, PASSWORD);
            List<Bukimedia.PrestaSharp.Entities.address> all = addresses.GetAll();

            foreach (Bukimedia.PrestaSharp.Entities.address a in all)
            {
                if (a.id_customer == id)
                {
                    return a;
                }
            }
            return null;
        }
    }
}
