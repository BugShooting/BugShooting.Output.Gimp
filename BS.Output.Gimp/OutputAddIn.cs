using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;

namespace BS.Output.Gimp
{
  public class OutputAddIn: V3.OutputAddIn<Output>
  {

    protected override string Name
    {
      get { return "Gimp"; }
    }

    protected override Image Image64
    {
      get  { return Properties.Resources.logo_64; }
    }

    protected override Image Image16
    {
      get { return Properties.Resources.logo_16 ; }
    }

    protected override bool Editable
    {
      get { return true; }
    }

    protected override string Description
    {
      get { return "Open screenshots in Gimp for direct editing."; }
    }
    
    protected override Output CreateOutput(IWin32Window Owner)
    {

      Output output = new Output(Name,
                                 "Screenshot",
                                 String.Empty,
                                 false);

      return EditOutput(Owner, output);

    }

    protected override Output EditOutput(IWin32Window Owner, Output Output)
    {

      Edit edit = new Edit(Output);

      var ownerHelper = new System.Windows.Interop.WindowInteropHelper(edit);
      ownerHelper.Owner = Owner.Handle;

      if (edit.ShowDialog() == true)
      {

        return new Output(edit.OutputName,
                          edit.FileName,
                          edit.FileFormat,
                          edit.EditFileName);
      }
      else
      {
        return null;
      }

    }

    protected override OutputValueCollection SerializeOutput(Output Output)
    {

      OutputValueCollection outputValues = new OutputValueCollection();

      outputValues.Add(new OutputValue("Name", Output.Name));
      outputValues.Add(new OutputValue("FileName", Output.FileName));
      outputValues.Add(new OutputValue("FileFormat", Output.FileFormat));
      outputValues.Add(new OutputValue("EditFileName", Output.EditFileName.ToString()));

      return outputValues;

    }

    protected override Output DeserializeOutput(OutputValueCollection OutputValues)
    {
      return new Output(OutputValues["Name", this.Name].Value,
                        OutputValues["FileName", "Screenshot"].Value,
                        OutputValues["FileFormat", ""].Value,
                        Convert.ToBoolean(OutputValues["EditFileName", false.ToString()].Value));
    }

    protected async override Task<V3.SendResult> Send(IWin32Window Owner, Output Output, V3.ImageData ImageData)
    {
      try
      {

        string applicationPath = string.Empty;

        using (RegistryKey localMachineKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
        {
          using (RegistryKey classKey = localMachineKey.OpenSubKey("Software\\Classes\\.xcf", false))
          {
            if (classKey != null)
            {

              string classValue = Convert.ToString(classKey.GetValue(string.Empty, string.Empty));

              using (RegistryKey commandKey = localMachineKey.OpenSubKey("Software\\Classes\\" + classValue + "\\shell\\open\\command", false))
              {
                if (commandKey != null)
                {

                  string openCommand = Convert.ToString(commandKey.GetValue(string.Empty, string.Empty));

                  applicationPath = openCommand.Split(new char[] { Convert.ToChar("\"") }, StringSplitOptions.RemoveEmptyEntries)[0];

                }
              }

            }
          }
        }

        if (!File.Exists(applicationPath))
        {
          return new V3.SendResult(V3.Result.Failed, "Gimp is not installed.");
        }


        string fileName = V3.FileHelper.GetFileName(Output.FileName, Output.FileFormat, ImageData);

        if (Output.EditFileName)
        {

          Send send = new Send(fileName);

          var ownerHelper = new System.Windows.Interop.WindowInteropHelper(send);
          ownerHelper.Owner = Owner.Handle;

          if (send.ShowDialog() != true)
          {
            return new V3.SendResult(V3.Result.Canceled);
          }

          fileName = send.FileName;

        }

        string filePath = Path.Combine(Path.GetTempPath(), fileName + "." + V3.FileHelper.GetFileExtention(Output.FileFormat));

        Byte[] fileBytes = V3.FileHelper.GetFileBytes(Output.FileFormat, ImageData);

        using (FileStream file = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite))
        {
          file.Write(fileBytes, 0, fileBytes.Length);
          file.Close();
        }

        Process.Start(applicationPath, "\"" + filePath + "\"");

        return new V3.SendResult(V3.Result.Success);

      }
      catch (Exception ex)
      {
        return new V3.SendResult(V3.Result.Failed, ex.Message);
      }

    }

  }

}