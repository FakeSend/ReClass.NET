﻿using ReClassNET.DataExchange;
using ReClassNET.Gui;
using ReClassNET.Nodes;
using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ReClassNET
{
	partial class MainForm : Form
	{
		private readonly NativeHelper nativeHelper;
		private readonly Settings settings;

		private readonly RemoteProcess remoteProcess;
		private readonly Memory memory;

		public MainForm(NativeHelper nativeHelper, Settings settings)
		{
			Contract.Requires(nativeHelper != null);
			Contract.Requires(settings != null);

			this.nativeHelper = nativeHelper;
			this.settings = settings;

			InitializeComponent();

			mainMenuStrip.Renderer = new CustomToolStripProfessionalRenderer(true);
			toolStrip.Renderer = new CustomToolStripProfessionalRenderer(false);

			remoteProcess = new RemoteProcess(nativeHelper);
			remoteProcess.ProcessChanged += delegate (RemoteProcess sender)
			{
				if (sender.Process == null)
				{
					processToolStripStatusLabel.Text = "No process selected";
				}
				else
				{
					processToolStripStatusLabel.Text = $"{sender.Process.Name} (PID: {sender.Process.Id})";
				}
			};

			memory = new Memory
			{
				Process = remoteProcess
			};

			ClassNode.NewClassCreated += delegate (ClassNode node)
			{
				classesView.Add(node);
			};

			memoryViewControl.Settings = settings;
			memoryViewControl.Memory = memory;

			newClassToolStripButton_Click(null, null);
		}

		private void selectProcessToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using (var pb = new ProcessBrowser(nativeHelper, settings.LastProcess))
			{
				if (pb.ShowDialog() == DialogResult.OK)
				{
					if (remoteProcess.Process != null)
					{
						nativeHelper.CloseRemoteProcess(remoteProcess.Process.Handle);
					}

					remoteProcess.Process = pb.SelectedProcess;

					settings.LastProcess = remoteProcess.Process.Name;
				}
			}
		}

		private void classesView_ClassSelected(object sender, ClassNode node)
		{
			memoryViewControl.ClassNode = node;

			memoryViewControl.Invalidate();
		}

		private void cleanUnusedClassesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			classesView.RemoveUnusedClasses();
		}

		private void addBytesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var item = sender as IntegerToolStripMenuItem;
			if (item == null)
			{
				return;
			}

			memoryViewControl.AddBytes(item.Value);
		}

		private void insertBytesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var item = sender as IntegerToolStripMenuItem;
			if (item == null)
			{
				return;
			}

			memoryViewControl.InsertBytes(item.Value);
		}

		private void memoryTypeToolStripButton_Click(object sender, EventArgs e)
		{
			var item = sender as TypeToolStripButton;
			if (item == null)
			{
				return;
			}

			memoryViewControl.ReplaceSelectedNodesWithType(item.Value);
		}

		private void newClassToolStripButton_Click(object sender, EventArgs e)
		{
			var node = new ClassNode();
			node.AddBytes(64);

			classesView.SelectedClass = node;
		}

		private void openProjectToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using (var ofd = new OpenFileDialog())
			{
				ofd.CheckFileExists = true;
				ofd.Filter = $"{ReClassNetFile.FormatName} (*{ReClassNetFile.FileExtension})|*{ReClassNetFile.FileExtension}"
					+ $"|{ReClassQtFile.FormatName} (*{ReClassQtFile.FileExtension})|*{ReClassQtFile.FileExtension}"
					+ $"|{ReClassFile.FormatName} (*{ReClassFile.FileExtension})|*{ReClassFile.FileExtension}"
					/*+ $"|{ReClass2007File.FormatName} (*{ReClass2007File.FileExtension})|*{ReClass2007File.FileExtension}"*/;

				if (ofd.ShowDialog() == DialogResult.OK)
				{
					IReClassImport import = null;
					switch (Path.GetExtension(ofd.SafeFileName))
					{
						case ReClassNetFile.FileExtension:
							import = new ReClassNetFile();
							break;
						case ReClassQtFile.FileExtension:
							import = new ReClassQtFile();
							break;
						case ReClassFile.FileExtension:
							import = new ReClassFile();
							break;
						/*case ReClass2007File.FileExtension:
							import = new ReClass2007File();
							break;*/
					}
					if (import != null)
					{
						var sb = new StringBuilder();

						var schema = import.Load(ofd.FileName, s => sb.AppendLine(s));

						if (sb.Length != 0)
						{
							MessageBox.Show("Errors occurred during import:\n\n" + sb.ToString(), "Error");
						}

						if (schema != null)
						{
							classesView.Clear();

							var classes = schema.BuildNodes();
							classes.ForEach(c => classesView.Add(c));
							memoryViewControl.ClassNode = classes.FirstOrDefault();
						}
					}
				}
			}
		}

		private void memoryViewerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			new ProcessMemoryViewer(remoteProcess, classesView).Show();
		}

		private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using (var sd = new SettingsDialog(settings))
			{
				sd.ShowDialog();
			}
		}

		private void newClassToolStripMenuItem_Click(object sender, EventArgs e)
		{

		}
	}
}