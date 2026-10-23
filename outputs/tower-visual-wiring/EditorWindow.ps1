param(
    [ValidateSet('Focus','Refresh','PlayToggle','BuildTower','Capture','Click','Keys')][string]$Action='Capture',
    [string]$OutputName='editor.png', [int]$X=0, [int]$Y=0, [string]$Keys
)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition @'
using System;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
public static class ProjectEditorWindow {
 public delegate bool Callback(IntPtr h,IntPtr p);
 [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left,Top,Right,Bottom; }
 [DllImport("user32.dll")] public static extern bool EnumWindows(Callback c,IntPtr p);
 [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h,out uint p);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr h,StringBuilder s,int n);
 [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
 [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h,out Rect r);
 [DllImport("user32.dll")] public static extern bool SetCursorPos(int x,int y);
 [DllImport("user32.dll")] public static extern void mouse_event(uint f,uint x,uint y,uint d,UIntPtr e);
 public static IntPtr Find() {
  var windows=new List<IntPtr>();
  EnumWindows((h,p)=>{uint id;GetWindowThreadProcessId(h,out id);try{if(Process.GetProcessById((int)id).ProcessName.Equals("Unity",StringComparison.OrdinalIgnoreCase)&&IsWindowVisible(h)){var s=new StringBuilder(1024);GetWindowText(h,s,1024);if(s.ToString().Contains("Project-A-SquareTowerDefense"))windows.Add(h);}}catch{}return true;},IntPtr.Zero);
  if(windows.Count!=1)throw new Exception("Project editor window count: "+windows.Count);
  return windows[0];
 }
}
'@
$handle=[ProjectEditorWindow]::Find()
if (![ProjectEditorWindow]::SetForegroundWindow($handle)) { throw 'Cannot focus project editor' }
Start-Sleep -Milliseconds 250
$rect=New-Object ProjectEditorWindow+Rect
[void][ProjectEditorWindow]::GetWindowRect($handle,[ref]$rect)
switch($Action) {
 'Keys' { [Windows.Forms.SendKeys]::SendWait($Keys) }
 'Refresh' { [Windows.Forms.SendKeys]::SendWait('^r') }
 'PlayToggle' { [Windows.Forms.SendKeys]::SendWait('^p') }
 'BuildTower' { [Windows.Forms.SendKeys]::SendWait('b') }
 'Click' {
  [void][ProjectEditorWindow]::SetCursorPos($rect.Left+$X,$rect.Top+$Y)
  [ProjectEditorWindow]::mouse_event(2,0,0,0,[UIntPtr]::Zero)
  [ProjectEditorWindow]::mouse_event(4,0,0,0,[UIntPtr]::Zero)
 }
 'Capture' {
  $width=$rect.Right-$rect.Left; $height=$rect.Bottom-$rect.Top
  $bmp=New-Object Drawing.Bitmap($width,$height)
  $g=[Drawing.Graphics]::FromImage($bmp)
  $g.CopyFromScreen($rect.Left,$rect.Top,0,0,$bmp.Size)
  $output=Join-Path $PSScriptRoot $OutputName
  $bmp.Save($output,[Drawing.Imaging.ImageFormat]::Png)
  $g.Dispose();$bmp.Dispose()
  Write-Output $output
 }
}
Write-Output ("Action={0}; editor=({1},{2}) {3}x{4}" -f $Action,$rect.Left,$rect.Top,($rect.Right-$rect.Left),($rect.Bottom-$rect.Top))
