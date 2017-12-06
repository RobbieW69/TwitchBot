using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
//using System.Linq; //Adds all the linq specific methods to intellisense
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace TwitchBot
{
    public partial class Form1 : Form
    {

        TcpClient tcpClient; //new Transmission Control Protocol
        StreamReader sreader; 
        StreamWriter swriter;
        string pwd = File.ReadAllText("password.txt");
        string username = "robbiew_yt";
        string chatInfo;
        Dictionary<string, string> iMessages = new Dictionary<string, string>();
        List<string> bannedUsers = new List<string>();
        List<string> mods = new List<string>();
        string modsFile;
        string[] toadd;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            #region//Twitch stuff
            //We've connected to twitch servers
            tcpClient = new TcpClient("irc.chat.twitch.tv", 6667); //Connection/Stream
            if (tcpClient.Connected)
            {
                sreader = new StreamReader(tcpClient.GetStream());
                swriter = new StreamWriter(tcpClient.GetStream());
                
                //Request connect for access to User with pwd
                swriter.WriteLine($"PASS {pwd}\nNICK {username}\nUSER {username} 8 * :{username}");
                //Join a channel, channel is twitch.tv/(channel name here)
                swriter.WriteLine($"JOIN #{username}");
                swriter.Flush();
                //Should be connected
                chatInfo = $":{username}!{username}@{username}.tmi.twitch.tv PRIVMSG #{username} :";
            }
            else //Connection didn't succeed
            {
                tcpClient = new TcpClient("irc.chat.twitch.tv", 6667);
            }
            #endregion
        }

        /// <summary>
        /// Every time the timer ticks, once = 100ms
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            modsFile = File.ReadAllText("moderators.txt");
            //modsfile = "darpa,shadow"
            toadd = modsFile.Split(',');
            for (int i = 0; i < toadd.Length; i++)
            {
                if (!mods.Contains(toadd[i]))
                {
                  mods.Add(toadd[i]);
                }
            }
            //If we're recieving at least 1 byte of information from twitch
            if (tcpClient.Available > 0 || sreader.Peek() >= 0)
            {
                string msg = sreader.ReadLine(); //Gets the data from the channel from twitch
                if (msg.Contains("PRIVMSG"))
                {
                    string user = msg.Split('!')[0].TrimStart(':');//User/Sender
                    string message = msg.Split(':')[2];//Message
                    string output = user.Insert(user.Length, ":").Insert(user.Length + 1, message);//uname:msg
                    chatLabel.Text += $"\n{output}";//Display the message to bot
                    if (message.StartsWith("!"))
                    {
                        message = message.ToLower();
                        Commands(message, user);
                        /*The moderators of this room are: darpachieff, shadowofcobra1, yoyo"*/
                    }
                }
                else
                {
                    chatLabel.Text += $"\n{msg}";
                }
            }
        }

        /// <summary>
        /// Process commands from chat
        /// </summary>
        /// <param name="msg">Command from chat</param>
        void Commands(string msg, string sender)
        {
            #region//Long cmds
            if (msg.StartsWith("!send"))
            {
                string importantMessage = msg.Substring(6);
                //= !send message 
                string msgg = importantMessage + "(" + DateTime.Now.ToLongTimeString()+" "
                    + DateTime.Now.ToShortDateString()+")";
                if (!iMessages.ContainsKey(sender))
                {
                    iMessages.Add(sender, msgg);
                    SendMessage("Thankyou for your important message.", sender);
                }
                else
                {
                    //If they try to send more than one important message
                    SendMessage("Sorry but you can only send ONE important message to me.", sender);
                }
            }
            #endregion

            #region//Ez cmds
            if (msg == "!list")
            {
                if (bannedUsers.Count > 0)
                {
                    for (int i = 0; i < bannedUsers.Count; i++)
                    {
                        int n = i + 1;
                        SendMessage("Banlist: #" + n + " " + bannedUsers[i]);
                    }
                }
                else
                {
                    SendMessage("Banlist:Empty");
                }
                
            }
            if (msg == "!mlist")
            {
                if (mods.Count > 0)
                {
                    for (int i = 0; i < mods.Count; i++)
                    {
                        string mod = mods[i];
                        if (!string.IsNullOrEmpty(mod))
                        { 
                            SendMessage($"Mod: {i+1}.{mod}");
                        }
                        else
                        {
                            SendMessage($"Moderator in list cannot be null");
                        }
                        
                    }
                }
                else
                {
                    SendMessage($"There are no moderators on the list.");
                }
            }
            #endregion
        }

        void ClientCommands(string msg)
        {
            msg = msg.ToLower();
            //!BAN SDIJKLHFBGSDIHFBSDFIKUHB
            //!ban jkldsfjlisfnsdjif
            #region//Ez Cmds
            if (msg == "!shutdown")
            {
                Application.Exit();
            }
            if (msg == "!hello")
            {
                SendMessage("Hello chat from bot :)");
            }
            if (msg == "!inbox")
            {
                foreach (var item in iMessages)
                {

                    string user = item.Key;
                    string message = item.Value;
                    if (iMessages.ContainsKey(user))
                    {
                        chatLabel.Text += $"\n{user}:{message}";
                    }
                    else
                        chatBox.Text += $"We don't have a message from {user}";
                }
            }
            if (msg == "!clear")
            {
                SendMessage("/clear");
            }
            if (msg == "!list")
            {
                if (bannedUsers.Count > 0)
                {
                    for (int i = 0; i < bannedUsers.Count; i++)
                    {
                        int n = i + 1;
                        SendMessage("Banlist: #" + n + " " + bannedUsers[i]);
                    }
                }
                else
                {
                    SendMessage("Banlist:Empty");
                }
            }
            #endregion

            #region//Harder cmds
            if (msg.StartsWith("!ban"))
            {
                string user = msg.Split(' ')[1]; //!ban user
                if (!bannedUsers.Contains(user))
                {
                    SendMessage($"/ban {user}");
                    SendMessage($"User: '{user}' was banned by a mod.");
                    bannedUsers.Add(user);
                }
                else
                {
                    chatBox.Text += $"{user} is already on the banlist.";
                }
            }
            if (msg.StartsWith("!unban"))
            {
                string user = msg.Split(' ')[1]; //User
                if (bannedUsers.Contains(user))
                {
                    SendMessage($"/unban {user}");
                    SendMessage($"User: {user} was unbanned by a mod.");
                    bannedUsers.Remove(user);
                }
                else //if the user isnt in the list
                {
                    chatBox.Text += $"User: {user} is not in the banlist.";
                }
            }
            if (msg.StartsWith("!mod"))
            {
                string user = msg.Split(' ')[1]; //!mod uname
                if (!mods.Contains(user))
                {
                    SendMessage($"/mod {user}");
                    SendMessage($"User: {user} has been made a moderator.");
                    mods.Add(user);
                }
                else
                    chatBox.Text += user + " is already a mod.";
            }
            if (msg.StartsWith("!unmod"))
            {
                string user = msg.Split(' ')[1]; //!mod uname
                if (mods.Contains(user))
                {
                    SendMessage($"/unmod {user}");
                    SendMessage($"User: {user} has been deranked.");
                    mods.Remove(user);
                }
                else
                    chatBox.Text += user + " is not a mod.";
            }
            #endregion
        }

        void SendMessage(string msg)
        {
            swriter.WriteLine($"{chatInfo} {msg}");
            swriter.Flush();
        }

        void SendMessage(string msg, string sender)
        {
            swriter.WriteLine($"{chatInfo}@{sender} {msg}");
            swriter.Flush();
        }

        private void chatBox_KeyDown(object sender, KeyEventArgs e)
        {
            string msg = chatBox.Text;
            if (e.KeyCode == Keys.Enter)
            {
                if (chatBox.Text != null)
                {
                    if (!string.IsNullOrEmpty(chatBox.Text) && !chatBox.Text.StartsWith("!"))
                    {
                        //If the message is a regular message and not a command
                        SendMessage($"{msg}");
                        chatBox.Text = string.Empty;
                        e.SuppressKeyPress = true;
                    }
                    else if (msg.StartsWith("!"))
                    {
                        //Process client side cmds
                        ClientCommands(msg);
                        chatBox.Text = "";
                        e.SuppressKeyPress = true;
                    }
                }
            }
        }
    }
}
