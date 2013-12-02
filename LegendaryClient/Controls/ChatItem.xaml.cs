﻿using LegendaryClient.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LegendaryClient.Controls
{
    /// <summary>
    /// Interaction logic for ChatItem.xaml
    /// </summary>
    public partial class ChatItem : UserControl
    {
        public ChatItem()
        {
            InitializeComponent();
        }

        public void Update()
        {
            ChatText.Document.Blocks.Clear();
            ChatPlayerItem tempItem = null;
            foreach (KeyValuePair<string, ChatPlayerItem> x in Client.AllPlayers)
            {
                if (x.Value.Username == (string)Client.ChatItem.PlayerLabelName.Content)
                {
                    tempItem = x.Value;
                    break;
                }
            }

            foreach (string x in tempItem.Messages)
            {
                string[] Message = x.Split('|');
                TextRange tr = new TextRange(ChatText.Document.ContentEnd, ChatText.Document.ContentEnd);
                if (Message[0] == tempItem.Username)
                {
                    tr.Text = tempItem.Username + ": ";
                    tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Gold);
                }
                else
                {
                    tr.Text = Message[0] + ": ";
                    tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.SteelBlue);
                }
                tr = new TextRange(ChatText.Document.ContentEnd, ChatText.Document.ContentEnd);
                tr.Text = x.Replace(Message[0] + "|", "") + Environment.NewLine;
                tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.White);
            }

            ChatText.ScrollToEnd();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            Client.MainGrid.Children.Remove(Client.ChatItem);
            Client.ChatItem = null;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            NotificationChatPlayer tempPlayer = null;

            foreach (NotificationChatPlayer x in Client.ChatListView.Items)
            {
                if (x.PlayerLabelName.Content == Client.ChatItem.PlayerLabelName.Content)
                {
                    tempPlayer = x;
                    break;
                }
            }

            Client.MainGrid.Children.Remove(Client.ChatItem);
            Client.ChatItem = null;

            Client.ChatListView.Items.Remove(tempPlayer);
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            TextRange tr = new TextRange(ChatText.Document.ContentEnd, ChatText.Document.ContentEnd);
            tr.Text = Client.LoginPacket.AllSummonerData.Summoner.Name + ": ";
            tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.SteelBlue);
            tr = new TextRange(ChatText.Document.ContentEnd, ChatText.Document.ContentEnd);
            tr.Text = ChatTextBox.Text + Environment.NewLine;
            tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.White);

            ChatPlayerItem tempItem = null;
            string JID = "";
            foreach (KeyValuePair<string, ChatPlayerItem> x in Client.AllPlayers)
            {
                if (x.Value.Username == (string)Client.ChatItem.PlayerLabelName.Content)
                {
                    tempItem = x.Value;
                    JID = x.Key + "@pvp.net";
                    break;
                }
            }
            tempItem.Messages.Add(Client.LoginPacket.AllSummonerData.Summoner.Name + "|" + ChatTextBox.Text);
            ChatText.ScrollToEnd();

            Client.ChatClient.Message(JID, ChatTextBox.Text);

            ChatTextBox.Text = "";
        }
    }
}
