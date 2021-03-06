﻿Imports System.IO
Imports Microsoft.Win32
Imports Display_Driver_Uninstaller.Win32
Imports System.Security.AccessControl
Imports System.ServiceProcess
Imports System.Threading
Imports Windows.Foundation
Imports Windows.Management.Deployment
Imports Windows


Public Class CleanupEngine

	'	Private win8higher As Boolean = frmMain.win8higher

	Private Function UpdateTextMethodmessagefn(ByRef number As Integer) As String
		Return Languages.GetTranslation("frmMain", "UpdateLog", String.Format("Text{0}", number + 1))
	End Function


	Public Sub Deletesubregkey(ByRef regkeypath As RegistryKey, ByVal child As String)
		Dim fixregacls As Boolean = False
		If (regkeypath IsNot Nothing) AndAlso (Not IsNullOrWhitespace(child)) Then
			Try
				Using regkey As RegistryKey = MyRegistry.OpenSubKey(regkeypath, child, True)
					'we do this simply to ensure that the permissions are set to open this registrykey.
					'or else we will get an argument exception when trying to remove the key if permission are wrong.
					If regkey IsNot Nothing Then
						For Each childs As String In regkey.GetSubKeyNames
							If IsNullOrWhitespace(childs) Then Continue For
							Deletesubregkey(regkey, childs)
						Next
					End If
				End Using
				regkeypath.DeleteSubKeyTree(child)
				Application.Log.AddMessage(regkeypath.ToString & "\" & child & " - " & UpdateTextMethodmessagefn(39))
			Catch ex As UnauthorizedAccessException
				Application.Log.AddWarningMessage("Failed to remove registry subkey " + child + " Will try to set ACLs permission and try again.")
				fixregacls = True
			End Try
			'If exists, it means we need to modify it's ACls.
			If fixregacls AndAlso (regkeypath IsNot Nothing) Then
				ACL.Addregistrysecurity(regkeypath, child, RegistryRights.FullControl, AccessControlType.Allow)
				Try
					regkeypath.DeleteSubKeyTree(child)
				Catch ex As Exception
					Application.Log.AddWarning(ex, " Failed or already removed with another Thread ? " & child)
				End Try
				Application.Log.AddMessage(child + " - " + UpdateTextMethodmessagefn(39))
			End If
		End If
	End Sub



	Public Sub RemoveSharedDlls(ByVal directorypath As String)
		If Not IsNullOrWhitespace(directorypath) AndAlso Not FileIO.ExistsDir(directorypath) Then
			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\Folders", True)
				If regkey IsNot Nothing Then
					Try
						Deletevalue(regkey, If(Not directorypath.EndsWith("\"), directorypath & "\", directorypath))
					Catch exARG As ArgumentException
						'nothing to do,it probably doesn't exit.
					Catch ex As Exception
						Application.Log.AddException(ex)
					End Try
				End If
			End Using

			If Not directorypath.EndsWith("\") Then
				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Microsoft\Windows\CurrentVersion\SharedDLLs", True)
					If regkey IsNot Nothing Then
						Try
							Deletevalue(regkey, directorypath)
						Catch exARG As ArgumentException
							'nothing to do,it probably doesn't exit.
						Catch ex As Exception
							Application.Log.AddException(ex)
						End Try
					End If
				End Using

				If IntPtr.Size = 8 Then
					Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Wow6432Node\Microsoft\Windows\CurrentVersion\SharedDLLs", True)
						If regkey IsNot Nothing Then
							Try
								Deletevalue(regkey, directorypath)
							Catch exARG As ArgumentException
								'nothing to do,it probably doesn't exit.
							Catch ex As Exception
								Application.Log.AddException(ex)
							End Try
						End If
					End Using
				End If
			End If
		End If

	End Sub

	Public Sub RemoveAppx(ByVal AppxToRemove As String)
		Dim win10 As Boolean = frmMain.win10
		Dim WasRemoved As Boolean = False
		If win10 Then
			If Security.Principal.WindowsIdentity.GetCurrent().IsSystem Then
				ImpersonateLoggedOnUser.ReleaseToken()  'Will not work if we impersonate "SYSTEM"
				'ACL.AddPriviliges(ACL.SE.SECURITY_NAME, ACL.SE.BACKUP_NAME, ACL.SE.RESTORE_NAME, ACL.SE.TAKE_OWNERSHIP_NAME, ACL.SE.TCB_NAME, ACL.SE.CREATE_TOKEN_NAME)
			End If

			Try
				Select Case System.Windows.Forms.SystemInformation.BootMode
					Case System.Windows.Forms.BootMode.FailSafe
						Try
							Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Control\SafeBoot\Minimal", True)
								If regkey IsNot Nothing Then
									Using regSubKey As RegistryKey = regkey.CreateSubKey("AppXSvc", RegistryKeyPermissionCheck.ReadWriteSubTree)
										regSubKey.SetValue("", "Service")
									End Using
									Using regSubKey As RegistryKey = regkey.CreateSubKey("camsvc", RegistryKeyPermissionCheck.ReadWriteSubTree)
										regSubKey.SetValue("", "Service")
									End Using
									Using regSubKey As RegistryKey = regkey.CreateSubKey("clipSVC", RegistryKeyPermissionCheck.ReadWriteSubTree)
										regSubKey.SetValue("", "Service")
									End Using
									Using regSubKey As RegistryKey = regkey.CreateSubKey("Wsearch", RegistryKeyPermissionCheck.ReadWriteSubTree)
										regSubKey.SetValue("", "Service")
									End Using
								End If
							End Using
						Catch ex As Exception
							Application.Log.AddException(ex, "Failed to set '\SafeBoot\Minimal' RegistryKey for APPXSvc,etc...!")
						End Try
					Case System.Windows.Forms.BootMode.FailSafeWithNetwork
						Try
							Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Control\SafeBoot\Network", True)
								If regkey IsNot Nothing Then
									Using regSubKey As RegistryKey = regkey.CreateSubKey("AppXSvc", RegistryKeyPermissionCheck.ReadWriteSubTree)
										regSubKey.SetValue("", "Service")
									End Using
									Using regSubKey As RegistryKey = regkey.CreateSubKey("camsvc", RegistryKeyPermissionCheck.ReadWriteSubTree)
										regSubKey.SetValue("", "Service")
									End Using
									Using regSubKey As RegistryKey = regkey.CreateSubKey("clipSVC", RegistryKeyPermissionCheck.ReadWriteSubTree)
										regSubKey.SetValue("", "Service")
									End Using
									Using regSubKey As RegistryKey = regkey.CreateSubKey("Wsearch", RegistryKeyPermissionCheck.ReadWriteSubTree)
										regSubKey.SetValue("", "Service")
									End Using
								End If
							End Using
						Catch ex As Exception
							Application.Log.AddException(ex, "Failed to set '\SafeBoot\Minimal' RegistryKey for APPXSvc,etc...!")
						End Try
				End Select


				'Dim opCompletedEvent As ManualResetEvent = New ManualResetEvent(False)
				'			End Function
				'Windows.Foundation.IAsyncAction() = packageManager.RemovePackageAsync("NVIDIACorp.NVIDIAControlPanel_8.1.949.0_x64__56jybvy8sckqj")
				Dim DeploymentEnded As Boolean = False
				Dim packageManager As PackageManager = New PackageManager()
				Dim packages As IEnumerable(Of ApplicationModel.Package) = CType(PackageManager.FindPackages(), IEnumerable(Of ApplicationModel.Package))

				For Each package In packages
					If package IsNot Nothing Then
						If IsNullOrWhitespace(package.Id.FullName) Then Continue For
						If StrContainsAny(package.Id.FullName, True, AppxToRemove) Then
							Dim deploymentOperation As IAsyncOperationWithProgress(Of DeploymentResult, DeploymentProgress) = packageManager.RemovePackageAsync(package.Id.FullName)

							While Not DeploymentEnded

								If deploymentOperation.Status = Windows.Foundation.AsyncStatus.[Error] Then

									Application.Log.AddMessage("NVCP DCH remove failed.")
									DeploymentEnded = True
									WasRemoved = False
								ElseIf deploymentOperation.Status = Windows.Foundation.AsyncStatus.Completed Then

									Application.Log.AddMessage("NVCP DCH removed.")
									DeploymentEnded = True
									WasRemoved = True

								End If
							End While
							If WasRemoved Then

								'Win 10 (1809)
								Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Microsoft\Windows\CurrentVersion\DeviceSetup\InstalledPfns", True)
									If regkey IsNot Nothing Then
										For Each ValueName As String In regkey.GetValueNames
											If IsNullOrWhitespace(ValueName) Then Continue For
											If StrContainsAny(ValueName, True, package.Id.FamilyName) Then  'Not working need fixing
												Try
													Deletevalue(regkey, ValueName)
												Catch ex As Exception
													Application.Log.AddException(ex)
												End Try
											End If
										Next
									End If
								End Using

								'Win 10 (1803)
								For Each regkeyusers As String In Registry.Users.GetSubKeyNames
									If IsNullOrWhitespace(regkeyusers) Then Continue For
									Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, regkeyusers & "\Software\Microsoft\Windows\CurrentVersion\DeviceSetup\InstalledPfns", True)
										If regkey IsNot Nothing Then
											For Each ValueName As String In regkey.GetValueNames
												If IsNullOrWhitespace(ValueName) Then Continue For
												If StrContainsAny(ValueName, True, package.Id.FamilyName) Then  'Not working need fixing
													Try
														Deletevalue(regkey, ValueName)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try
												End If
											Next
										End If
									End Using
								Next
							End If
						End If
					End If
				Next

			Catch ex As Exception
				Application.Log.AddException(ex)
			Finally
				Select Case System.Windows.Forms.SystemInformation.BootMode
					Case System.Windows.Forms.BootMode.FailSafe
						StopService("AppXSvc")
						StopService("camsvc")
						StopService("clipSVC")
						StopService("Wsearch")
						Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Control\SafeBoot\Minimal", True)
							If regkey IsNot Nothing Then
								Try
									Deletesubregkey(regkey, "AppXSvc")
									Deletesubregkey(regkey, "camsvc")
									Deletesubregkey(regkey, "clipSVC")
									Deletesubregkey(regkey, "Wsearch")
								Catch ex As Exception
									Application.Log.AddException(ex, "Failed to remove '\SafeBoot\Minimal' RegistryKey (AppXSvc)!")
								End Try
							End If
						End Using

					Case System.Windows.Forms.BootMode.FailSafeWithNetwork
						StopService("AppXSvc")
						StopService("camsvc")
						StopService("clipSVC")
						StopService("Wsearch")
						Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Control\SafeBoot\Network", True)
							If regkey IsNot Nothing Then
								Try
									Deletesubregkey(regkey, "AppXSvc")
									Deletesubregkey(regkey, "camsvc")
									Deletesubregkey(regkey, "clipSVC")
									Deletesubregkey(regkey, "Wsearch")
								Catch ex As Exception
									Application.Log.AddException(ex, "Failed to remove '\SafeBoot\Minimal' RegistryKey (AppXSvc)!")
								End Try
							End If
						End Using
				End Select
			End Try
		End If
		If Not Security.Principal.WindowsIdentity.GetCurrent().IsSystem Then
			ImpersonateLoggedOnUser.Taketoken()
			ACL.AddPriviliges(ACL.SE.SECURITY_NAME, ACL.SE.BACKUP_NAME, ACL.SE.RESTORE_NAME, ACL.SE.TAKE_OWNERSHIP_NAME, ACL.SE.TCB_NAME, ACL.SE.CREATE_TOKEN_NAME)
		End If
	End Sub

	Public Sub Deletevalue(ByVal regkeypath As RegistryKey, ByVal child As String)
		If regkeypath IsNot Nothing AndAlso Not IsNullOrWhitespace(child) Then
			regkeypath.DeleteValue(child)

			Application.Log.AddMessage(regkeypath.ToString & "\" & child & " - " & UpdateTextMethodmessagefn(40))
		End If
	End Sub

	Public Sub ClassRoot(ByVal classroots As String())

		Dim wantedvalue As String = Nothing
		Dim wantedvalue2 As String = Nothing
		Dim appid As String = Nothing
		Dim typelib As String = Nothing

		Application.Log.AddMessage("Begin ClassRoot CleanUP")

		Try
			Using regkeyRoot As RegistryKey = Registry.ClassesRoot
				If regkeyRoot IsNot Nothing Then
					For Each child As String In regkeyRoot.GetSubKeyNames()
						If IsNullOrWhitespace(child) Then Continue For

						For Each croot As String In classroots
							If IsNullOrWhitespace(croot) Then Continue For

							If child.StartsWith(croot, StringComparison.OrdinalIgnoreCase) Then
								Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkeyRoot, child & "\CLSID")
									If regkey2 IsNot Nothing Then
										wantedvalue = TryCast(regkey2.GetValue("", String.Empty), String)

										If IsNullOrWhitespace(wantedvalue) Then Continue For

										Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(regkeyRoot, "CLSID\" & wantedvalue)
											If regkey3 IsNot Nothing Then
												appid = TryCast(regkey3.GetValue("AppID", String.Empty), String)

												If Not IsNullOrWhitespace(appid) Then

													Using regkey4 As RegistryKey = MyRegistry.OpenSubKey(regkeyRoot, "AppID", True)
														If regkey4 IsNot Nothing Then
															Try
																Deletesubregkey(regkey4, appid)
															Catch exARG As ArgumentException
																'Do nothing, can happen (Not found)
															Catch ex As Exception
																Application.Log.AddException(ex)
															End Try
														End If
													End Using
												End If
											End If
										End Using

										Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(regkeyRoot, "CLSID\" & wantedvalue & "\TypeLib")
											If regkey3 IsNot Nothing Then
												typelib = TryCast(regkey3.GetValue("", String.Empty), String)

												If Not IsNullOrWhitespace(typelib) Then
													Using regkey4 As RegistryKey = MyRegistry.OpenSubKey(regkeyRoot, "TypeLib", True)
														If regkey4 IsNot Nothing Then
															Try
																Deletesubregkey(regkey4, typelib)
															Catch exARG As ArgumentException
																'Do nothing, can happen (Not found)
															Catch ex As Exception
																Application.Log.AddException(ex)
															End Try
														End If
													End Using
												End If
											End If
										End Using

										Using crkey As RegistryKey = MyRegistry.OpenSubKey(regkeyRoot, "CLSID", True)
											If crkey IsNot Nothing Then
												For Each wantedvaluekey In crkey.GetSubKeyNames
													If IsNullOrWhitespace(wantedvaluekey) Then Continue For

													If StrContainsAny(wantedvaluekey, True, wantedvalue) Then
														Try
															Deletesubregkey(crkey, wantedvalue)
														Catch ex As Exception
															Application.Log.AddException(ex)
														End Try

														For Each childfile As String In regkeyRoot.GetSubKeyNames()
															If IsNullOrWhitespace(childfile) Then Continue For

															If childfile.EndsWith("file", StringComparison.OrdinalIgnoreCase) Then

																Using regkey5 As RegistryKey = MyRegistry.OpenSubKey(regkeyRoot, childfile)
																	If regkey5 IsNot Nothing Then
																		For Each shellEX As String In regkey5.GetSubKeyNames
																			If IsNullOrWhitespace(shellEX) Then Continue For

																			If StrContainsAny(shellEX, True, "shellex") Then
																				Using regkey6 As RegistryKey = MyRegistry.OpenSubKey(regkey5, shellEX & "\ContextMenuHandlers", True)
																					If regkey6 IsNot Nothing Then
																						For Each ShExt As String In regkey6.GetSubKeyNames
																							If IsNullOrWhitespace(ShExt) Then Continue For

																							If StrContainsAny(ShExt, True, "openglshext", "nvappshext") Then
																								Using regkey7 As RegistryKey = MyRegistry.OpenSubKey(regkey6, ShExt)
																									If regkey7 IsNot Nothing Then
																										wantedvalue2 = TryCast(regkey7.GetValue("", String.Empty), String)
																										If Not IsNullOrWhitespace(wantedvalue2) Then
																											If StrContainsAny(wantedvalue2, True, wantedvalue) Then
																												Try
																													Deletesubregkey(regkey6, ShExt)
																												Catch ex As Exception
																													Application.Log.AddException(ex)
																												End Try
																											End If
																										End If
																									End If
																								End Using
																							End If
																						Next
																					End If
																				End Using
																			End If
																		Next
																	End If
																End Using
															End If
														Next
													End If
												Next
											End If
										End Using


										'here I remove the mediafoundationkeys if present
										'f79eac7d-e545-4387-bdee-d647d7bde42a is the Ecnoder section. Same on all windows version.

										Using regkeyM As RegistryKey = MyRegistry.OpenSubKey(regkeyRoot, "MediaFoundation\Transforms", True)
											If regkeyM IsNot Nothing Then
												Try
													Deletesubregkey(regkeyM, (wantedvalue.Replace("{", "")).Replace("}", ""))
												Catch exARG As ArgumentException
													'Do nothing, can happen (Not found)
												Catch ex As Exception
													Application.Log.AddException(ex)
												End Try
											End If
										End Using


										Using regkeyM As RegistryKey = MyRegistry.OpenSubKey(regkeyRoot, "MediaFoundation\Transforms\Categories\f79eac7d-e545-4387-bdee-d647d7bde42a", True)
											If regkeyM IsNot Nothing Then
												Try
													Deletesubregkey(regkeyM, (wantedvalue.Replace("{", "")).Replace("}", ""))
												Catch exARG As ArgumentException
													'Do nothing, can happen (Not found)
												Catch ex As Exception
													Application.Log.AddException(ex)
												End Try
											End If
										End Using


										Using regkeyM As RegistryKey = MyRegistry.OpenSubKey(regkeyRoot, "MediaFoundation\Transforms\Categories\d6c02d4b-6833-45b4-971a-05a4b04bab91", True)
											If regkeyM IsNot Nothing Then
												Try
													Deletesubregkey(regkeyM, (wantedvalue.Replace("{", "")).Replace("}", ""))
												Catch exARG As ArgumentException
													'Do nothing, can happen (Not found)
												Catch ex As Exception
													Application.Log.AddException(ex)
												End Try
											End If
										End Using
									End If
								End Using

								Try
									Deletesubregkey(regkeyRoot, child)
								Catch ex As Exception
									Application.Log.AddException(ex)
								End Try
							End If
						Next
					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		If IntPtr.Size = 8 Then
			' DevMltk: I think there was typo?    Yes look like so. nice catch. (Wagnard)
			'
			' Orginal code:
			'
			'Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot,"Wow6432Node", True)		<--- regkey = 'HKEY_CLASSES_ROOT\Wow6432Node'
			'	If regkey IsNot Nothing Then
			'		
			'		For Each child As String In regkey.GetSubKeyNames()					<--- all subkeys of 'HKEY_CLASSES_ROOT\Wow6432Node'
			'			If IsNullOrWhitespace(child) = False Then
			'				For i As Integer = 0 To ClassRoot.Length - 1
			'					If Not IsNullOrWhitespace(ClassRoot(i)) Then
			'						If child.ToLower.StartsWith(ClassRoot(i).ToLower) Then
			'							Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot,child & "\CLSID")		 <-- ???
			'							
			'
			'	child is subkey of 'HKEY_CLASSES_ROOT\Wow6432Node'
			'		=> HKEY_CLASSES_ROOT\Wow6432Node\"child"
			'
			'	but ???
			'	Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot,child & "\CLSID")
			'		=> HKEY_CLASSES_ROOT\"child"\CLSID		<--- shouldn't child be under \Wow6432Node ?
			'
			'
			'	Using subregkey As RegistryKey = MyRegistry.OpenSubKey(regkey,child & "\CLSID")	<-- I Think this it should be, revert if I'm missing something there. Line 8311
			'		=> HKEY_CLASSES_ROOT\Wow6432Node\"child"\CLSID			

			Try
				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) Then Continue For

							For Each croot As String In classroots
								If IsNullOrWhitespace(croot) Then Continue For

								If child.StartsWith(croot, StringComparison.OrdinalIgnoreCase) Then
									Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child & "\CLSID")
										If regkey2 IsNot Nothing Then
											wantedvalue = TryCast(regkey2.GetValue("", String.Empty), String)

											If IsNullOrWhitespace(wantedvalue) Then Continue For

											Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(regkey, "CLSID\" & wantedvalue)
												If regkey3 IsNot Nothing Then
													appid = TryCast(regkey3.GetValue("AppID", String.Empty), String)

													If Not IsNullOrWhitespace(appid) Then

														Using regkey4 As RegistryKey = MyRegistry.OpenSubKey(regkey, "AppID", True)
															If regkey4 IsNot Nothing Then
																Try
																	Deletesubregkey(regkey4, appid)
																Catch exARG As ArgumentException
																	'Do nothing, can happen (Not found)
																Catch ex As Exception
																	Application.Log.AddException(ex)
																End Try
															End If
														End Using
													End If
												End If
											End Using

											Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(regkey, "CLSID\" & wantedvalue & "\TypeLib")
												If regkey3 IsNot Nothing Then
													typelib = TryCast(regkey3.GetValue("", String.Empty), String)

													If Not IsNullOrWhitespace(typelib) Then

														Using regkey4 As RegistryKey = MyRegistry.OpenSubKey(regkey, "TypeLib", True)
															If regkey4 IsNot Nothing Then
																Try
																	Deletesubregkey(regkey4, typelib)
																Catch exARG As ArgumentException
																	'Do nothing, can happen (Not found)
																Catch ex As Exception
																	Application.Log.AddException(ex)
																End Try
															End If
														End Using
													End If
												End If
											End Using

											Using regkeyC As RegistryKey = MyRegistry.OpenSubKey(regkey, "CLSID", True)
												If regkeyC IsNot Nothing Then
													Try
														Deletesubregkey(regkeyC, wantedvalue)
													Catch exARG As ArgumentException
														'Do nothing, can happen (Not found)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try
												End If
											End Using


											'here I remove the mediafoundationkeys if present
											'f79eac7d-e545-4387-bdee-d647d7bde42a is the Ecnoder section. Same on all windows version.

											Using regkeyM As RegistryKey = MyRegistry.OpenSubKey(regkey, "MediaFoundation\Transforms", True)
												If regkeyM IsNot Nothing Then
													Try
														Deletesubregkey(regkeyM, (wantedvalue.Replace("{", "")).Replace("}", ""))
													Catch exARG As ArgumentException
														'Do nothing, can happen (Not found)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try
												End If
											End Using

											Using regkeyM As RegistryKey = MyRegistry.OpenSubKey(regkey, "MediaFoundation\Transforms\Categories\f79eac7d-e545-4387-bdee-d647d7bde42a", True)
												If regkeyM IsNot Nothing Then
													Try
														Deletesubregkey(regkeyM, (wantedvalue.Replace("{", "")).Replace("}", ""))
													Catch exARG As ArgumentException
														'Do nothing, can happen (Not found)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try
												End If
											End Using



											Using regkeyM As RegistryKey = MyRegistry.OpenSubKey(regkey, "MediaFoundation\Transforms\Categories\d6c02d4b-6833-45b4-971a-05a4b04bab91", True)
												If regkeyM IsNot Nothing Then
													Try
														Deletesubregkey(regkeyM, (wantedvalue.Replace("{", "")).Replace("}", ""))
													Catch exARG As ArgumentException
														'Do nothing, can happen (Not found)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try
												End If
											End Using
										End If
									End Using

									Try
										Deletesubregkey(regkey, child)
									Catch ex As Exception
										Application.Log.AddException(ex)
									End Try
								End If
							Next
						Next
					End If
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try
		End If

		Application.Log.AddMessage("End ClassRoot CleanUP")
	End Sub

	Public Sub Installer(ByVal packages As String(), config As ThreadSettings)

		Dim wantedvalue As String = Nothing
		Dim removephysx As Boolean = config.RemovePhysX

		Try
			Application.Log.AddMessage("-Starting S-1-5-xx region cleanUP")
			Dim file As String
			Dim folder As String
			Using basekey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
			   "Software\Microsoft\Windows\CurrentVersion\Installer\UserData", False)
				If basekey IsNot Nothing Then
					For Each super As String In basekey.GetSubKeyNames()
						If IsNullOrWhitespace(super) Then Continue For

						If StrContainsAny(super, True, "s-1-5") Then

							Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
							 "Software\Microsoft\Windows\CurrentVersion\Installer\UserData\" & super & "\Products", True)

								If regkey IsNot Nothing Then
									For Each child As String In regkey.GetSubKeyNames()
										If IsNullOrWhitespace(child) Then Continue For

										Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
										"Software\Microsoft\Windows\CurrentVersion\Installer\UserData\" & super & "\Products\" & child &
										"\InstallProperties", False)

											If subregkey IsNot Nothing Then

												wantedvalue = TryCast(subregkey.GetValue("DisplayName", String.Empty), String)
												If IsNullOrWhitespace(wantedvalue) Then Continue For

												For Each package As String In packages
													If IsNullOrWhitespace(package) Then Continue For
													If (StrContainsAny(wantedvalue, True, package)) AndAlso
													  Not ((removephysx = False) AndAlso wantedvalue.ToLower.Contains("physx")) Then


														Application.Log.AddMessage("Removing .msi")
														'Deleting here the c:\windows\installer entries.
														Try
															file = TryCast(subregkey.GetValue("LocalPackage", String.Empty), String)
															If IsNullOrWhitespace(file) Then Continue For

															If StrContainsAny(file, True, ".msi") Then
																Delete(file)
															End If
														Catch ex As Exception
															Application.Log.AddException(ex)
														End Try

														Try
															folder = TryCast(subregkey.GetValue("UninstallString", String.Empty), String)
															If Not IsNullOrWhitespace(folder) Then
																If StrContainsAny(folder, True, "{") AndAlso StrContainsAny(folder, True, "}") Then

																	folder = folder.Substring(folder.IndexOf("{"), (folder.IndexOf("}") - folder.IndexOf("{")) + 1)
																	TestDelete(Environment.GetEnvironmentVariable("windir") + "\installer\" + folder, config)

																	Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\Folders", True)
																		If regkey2 IsNot Nothing Then

																			For Each subkeyname As String In regkey2.GetValueNames
																				If Not IsNullOrWhitespace(subkeyname) Then
																					If StrContainsAny(subkeyname, True, folder) Then
																						Deletevalue(regkey2, subkeyname)
																					End If
																				End If
																			Next
																		End If
																	End Using
																End If
															End If
														Catch ex As Exception
															Application.Log.AddException(ex)
														End Try

														Try
															Deletesubregkey(regkey, child)
														Catch ex As Exception
															Application.Log.AddException(ex)
														End Try

														Using superregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
														"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UpgradeCodes", True)
															If superregkey IsNot Nothing Then
																For Each child2 As String In superregkey.GetSubKeyNames()
																	If IsNullOrWhitespace(child2) Then Continue For

																	Using subsuperregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
																   "SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UpgradeCodes\" & child2, False)

																		If subsuperregkey IsNot Nothing Then
																			For Each wantedstring As String In subsuperregkey.GetValueNames()
																				If IsNullOrWhitespace(wantedstring) Then Continue For

																				If StrContainsAny(wantedstring, True, child) Then
																					Try
																						Deletesubregkey(superregkey, child2)
																					Catch ex As Exception
																						Application.Log.AddException(ex)
																					End Try
																				End If
																			Next
																		End If
																	End Using
																Next
															End If
														End Using
														Using superregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
														"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\" & super & "\Components", True)
															If superregkey IsNot Nothing Then
																For Each child2 As String In superregkey.GetSubKeyNames()
																	If IsNullOrWhitespace(child2) Then Continue For

																	Using subsuperregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
																   "SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\" & super & "\Components\" & child2, False)

																		If subsuperregkey IsNot Nothing Then
																			For Each wantedstring In subsuperregkey.GetValueNames()
																				If IsNullOrWhitespace(wantedstring) Then Continue For

																				If wantedstring.Contains(child) Then
																					Try
																						Deletesubregkey(superregkey, child2)
																					Catch ex As Exception
																						Application.Log.AddException(ex)
																					End Try
																				End If
																			Next
																		End If
																	End Using
																Next
															End If
														End Using
													End If
												Next
											End If
										End Using
									Next
								End If
							End Using
						End If
					Next
				End If
			End Using

			Application.Log.AddMessage("-End of S-1-5-xx region cleanUP")
		Catch ex As Exception
			MsgBox(Languages.GetTranslation("frmMain", "Messages", "Text6"))
			Application.Log.AddException(ex)
		End Try


		Try
			Dim folder As String
			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot,
			"Installer\Products", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) Then Continue For

						Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot,
						"Installer\Products\" & child, False)

							If subregkey IsNot Nothing Then

								wantedvalue = TryCast(subregkey.GetValue("ProductName", String.Empty), String)
								If IsNullOrWhitespace(wantedvalue) Then Continue For

								For Each package As String In packages
									If IsNullOrWhitespace(package) Then Continue For

									If StrContainsAny(wantedvalue, True, package) AndAlso
									   Not ((removephysx = False) AndAlso StrContainsAny(wantedvalue, True, "physx")) Then

										Try
											folder = TryCast(subregkey.GetValue("ProductIcon", String.Empty), String)

											If (IsNullOrWhitespace(folder)) Then Continue For
											If Not StrContainsAny(folder, True, "{") Then Continue For
											If Not StrContainsAny(folder, True, "}") Then Continue For

											folder = folder.Substring(folder.IndexOf("{"), (folder.IndexOf("}") - folder.IndexOf("{")) + 1)
											TestDelete(Environment.GetEnvironmentVariable("windir") + "\installer\" + folder, config)
											Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\Folders", True)
												If regkey2 IsNot Nothing Then
													For Each subkeyname As String In regkey2.GetValueNames
														If IsNullOrWhitespace(subkeyname) Then Continue For

														If StrContainsAny(subkeyname, True, folder) Then
															Deletevalue(regkey2, subkeyname)
														End If
													Next
												End If
											End Using

										Catch ex As Exception
											Application.Log.AddException(ex)
										End Try

										Try
											Deletesubregkey(regkey, child)
										Catch ex As Exception
										End Try

										Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Installer\Features", True)
											If regkey3 IsNot Nothing Then
												Try
													Deletesubregkey(regkey3, child)
												Catch ex As Exception
												End Try
											End If
										End Using

										Using superregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot,
										"Installer\UpgradeCodes", True)
											If superregkey IsNot Nothing Then
												For Each child2 As String In superregkey.GetSubKeyNames()
													If IsNullOrWhitespace(child2) Then Continue For

													Using subsuperregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot,
													  "Installer\UpgradeCodes\" & child2, False)

														If subsuperregkey IsNot Nothing Then
															For Each wantedstring As String In subsuperregkey.GetValueNames()
																If IsNullOrWhitespace(wantedstring) Then Continue For
																If wantedstring.Contains(child) Then
																	Try
																		Deletesubregkey(superregkey, child2)
																	Catch ex As Exception
																	End Try
																End If
															Next
														End If
													End Using
												Next
											End If
										End Using
									End If
								Next
							End If
						End Using
					Next
				End If
			End Using

		Catch ex As Exception
			MsgBox(Languages.GetTranslation("frmMain", "Messages", "Text6"))
			Application.Log.AddException(ex)
		End Try


		Try
			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
			"Software\Classes\Installer\Products", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) Then Continue For

						Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
						"Software\Classes\Installer\Products\" & child, False)

							If subregkey IsNot Nothing Then
								wantedvalue = TryCast(subregkey.GetValue("ProductName", String.Empty), String)
								If IsNullOrWhitespace(wantedvalue) Then Continue For

								For Each package As String In packages
									If IsNullOrWhitespace(package) Then Continue For

									If (StrContainsAny(wantedvalue, True, package)) AndAlso
									  Not ((removephysx = False) AndAlso wantedvalue.ToLower.Contains("physx")) Then

										Try
											Deletesubregkey(regkey, child)
										Catch ex As Exception
										End Try

										Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "Software\Classes\Installer\Features", True)
											If regkey2 IsNot Nothing Then
												Try
													Deletesubregkey(regkey2, child)
												Catch ex As Exception
												End Try
											End If
										End Using

										Using superregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
										"Software\Classes\Installer\UpgradeCodes", True)
											If superregkey IsNot Nothing Then
												For Each child2 As String In superregkey.GetSubKeyNames()
													If IsNullOrWhitespace(child2) Then Continue For

													Using subsuperregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine,
													  "Software\Classes\Installer\UpgradeCodes\" & child2, False)

														If subsuperregkey IsNot Nothing Then
															For Each wantedstring As String In subsuperregkey.GetValueNames()
																If IsNullOrWhitespace(wantedstring) Then Continue For

																If StrContainsAny(wantedstring, True, child) Then
																	Try
																		Deletesubregkey(superregkey, child2)
																	Catch ex As Exception
																	End Try
																End If
															Next
														End If
													End Using
												Next
											End If
										End Using
									End If
								Next
							End If
						End Using
					Next
				End If
			End Using

		Catch ex As Exception
			MsgBox(Languages.GetTranslation("frmMain", "Messages", "Text6"))
			Application.Log.AddException(ex)
		End Try


		Try
			For Each users As String In Registry.Users.GetSubKeyNames()
				If IsNullOrWhitespace(users) Then Continue For

				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users,
				users & "\Software\Microsoft\Installer\Products", True)

					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) Then Continue For

							Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users,
							  users & "\Software\Microsoft\Installer\Products\" & child, False)

								If subregkey IsNot Nothing Then
									wantedvalue = TryCast(subregkey.GetValue("ProductName", String.Empty), String)
									If IsNullOrWhitespace(wantedvalue) Then Continue For

									For Each package As String In packages
										If IsNullOrWhitespace(package) Then Continue For

										If (StrContainsAny(wantedvalue, True, package)) AndAlso
										   Not ((removephysx = False) AndAlso StrContainsAny(wantedvalue, True, "physx")) Then

											Try
												Deletesubregkey(regkey, child)
											Catch ex As Exception
											End Try

											Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(Registry.Users, users & "\Software\Microsoft\Installer\Features", True)
												If regkey2 IsNot Nothing Then
													Try
														Deletesubregkey(regkey2, child)
													Catch ex As Exception
													End Try
												End If
											End Using

											Using superregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users,
											users & "\Software\Microsoft\Installer\UpgradeCodes", True)
												If superregkey IsNot Nothing Then
													For Each child2 As String In superregkey.GetSubKeyNames()
														If IsNullOrWhitespace(child2) Then Continue For

														Using subsuperregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.Users,
														  users & "\Software\Microsoft\Installer\UpgradeCodes" & child2, False)

															If subsuperregkey IsNot Nothing Then
																For Each wantedstring As String In subsuperregkey.GetValueNames()
																	If IsNullOrWhitespace(wantedstring) Then Continue For

																	If wantedstring.Contains(child) Then
																		Try
																			Deletesubregkey(superregkey, child2)
																		Catch ex As Exception
																		End Try
																	End If
																Next
															End If
														End Using
													Next
												End If
											End Using
										End If
									Next
								End If
							End Using
						Next
					End If
				End Using
			Next

		Catch ex As Exception
			MsgBox(Languages.GetTranslation("frmMain", "Messages", "Text6"))
			Application.Log.AddException(ex)
		End Try

	End Sub

	Public Sub Cleanserviceprocess(ByVal services As String())
		Dim donotremoveamdhdaudiobusfiles = frmMain.donotremoveamdhdaudiobusfiles

		Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Services", False)
			If regkey IsNot Nothing Then
				For Each service As String In services
					If IsNullOrWhitespace(service) Then Continue For

					Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, service, False)
						If regkey2 IsNot Nothing Then
							If Not (donotremoveamdhdaudiobusfiles AndAlso StrContainsAny(service, True, "amdkmafd")) Then

								If ServiceInstaller.GetServiceStatus(service) = ServiceInstaller.SERVICE_STATE.NOT_FOUND Then
									'Service is not present
								Else
									Try
										ServiceInstaller.Uninstall(service)
									Catch ex As Exception
										Application.Log.AddException(ex)
										Continue For
									End Try


									Dim waits As Int32 = 0

									While waits < 30                         'MAX 3 sec APROX to wait Windows remove all files. ( 30 * 100ms)
										If ServiceInstaller.GetServiceStatus(service) <> ServiceInstaller.SERVICE_STATE.NOT_FOUND Then
											waits += 1
											System.Threading.Thread.Sleep(100)
										Else
											Exit While
										End If
									End While
									Using regkey4 As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\Setup\FirstBoot\Services", True)
										If regkey4 IsNot Nothing Then
											Try
												Deletesubregkey(regkey4, service)
											Catch exARG As ArgumentException
												'Do nothing, can happen (Not found)
											Catch ex As Exception
												Application.Log.AddException(ex)
											End Try
										End If
									End Using
								End If

								'Verify that the service was indeed removed via registry.
								Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(regkey, service, False)
									If regkey3 IsNot Nothing Then

										Application.Log.AddWarningMessage("Failed to remove the service : " & service)
									Else

										Application.Log.AddMessage("Service : " & service & " removed.")
									End If
								End Using
							End If
						End If
					End Using

					System.Threading.Thread.Sleep(10)
				Next
			End If
		End Using




		'-------------
		'control/video
		'-------------
		'Reason I put this in service is that the removal of this is based from its service.

		Try
			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Control\Video", True)
				If regkey IsNot Nothing Then
					Dim serviceValue As String

					For Each child As String In regkey.GetSubKeyNames
						If IsNullOrWhitespace(child) Then Continue For

						Using subregkey As RegistryKey = MyRegistry.OpenSubKey(regkey, child & "\Video", False)
							If subregkey IsNot Nothing Then
								serviceValue = TryCast(subregkey.GetValue("Service", String.Empty), String)

								If IsNullOrWhitespace(serviceValue) Then Continue For

								For Each service As String In services
									If IsNullOrWhitespace(service) Then Continue For
									If serviceValue.Equals(service, StringComparison.OrdinalIgnoreCase) Then
										Try
											Deletesubregkey(regkey, child)
											Deletesubregkey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Hardware Profiles\UnitedVideo\CONTROL\VIDEO\" & child)
											Exit For
										Catch ex As Exception
										End Try
									End If

								Next
							Else
								'Here, if subregkey is nothing, it mean \video doesnt exist and is no \0000, we can delete it.
								'this is a general cleanUP we could say.
								Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(regkey, child & "\0000")
									If regkey3 Is Nothing Then
										Try
											Deletesubregkey(regkey, child)
											Deletesubregkey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Hardware Profiles\UnitedVideo\CONTROL\VIDEO\" & child)
										Catch ex As Exception
										End Try
									End If
								End Using
							End If
						End Using
					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try
	End Sub

	Public Sub StartService(ByVal service As String)

		For Each svc As ServiceController In ServiceController.GetServices()
			Using svc
				If svc.ServiceName.Equals(service, StringComparison.OrdinalIgnoreCase) Then
					If svc.Status = ServiceControllerStatus.Stopped Then
						Try
							svc.Start()
							svc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(5))
						Catch ex As Exception
							Application.Log.AddException(ex)
						End Try
					End If
				End If
			End Using
		Next

	End Sub
	Public Function CheckServiceStartupType(ByVal service As String) As String
		Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Services\" & service, False)
			If regkey IsNot Nothing Then
				Return TryCast(regkey.GetValue("Start", String.Empty), String)
			End If
		End Using
		Return Nothing
	End Function

	Public Sub SetServiceStartupType(ByVal service As String, value As String)
		Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SYSTEM\CurrentControlSet\Services\" & service, True)
			If regkey IsNot Nothing Then
				regkey.SetValue("Start", value, RegistryValueKind.DWord)
			End If
		End Using
	End Sub

	Public Sub StopService(ByVal service As String)

		For Each svc As ServiceController In ServiceController.GetServices()
			Using svc
				If svc.ServiceName.Equals(service, StringComparison.OrdinalIgnoreCase) Then
					If svc.Status <> ServiceControllerStatus.Stopped Then
						Try
							svc.Stop()
							svc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(5))
						Catch ex As Exception
							Application.Log.AddException(ex)
						End Try

					End If
				End If
			End Using
		Next

	End Sub

	Public Sub DeleteService(ByVal service As String)
		Dim servicearray As String() = New String() {service}
		Cleanserviceprocess(servicearray)
	End Sub

	Public Sub PrePnplockdownfiles(ByVal oeminf As String)
		Dim win8higher = frmMain.win8higher
		Dim processinfo As New ProcessStartInfo
		Dim process As New Process
		Dim sourceValue As String
		Dim donotremoveamdhdaudiobusfiles = frmMain.donotremoveamdhdaudiobusfiles

		Try
			If win8higher Then
				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles", True)
					If regkey IsNot Nothing Then
						If Not IsNullOrWhitespace(oeminf) Then
							If Not (donotremoveamdhdaudiobusfiles AndAlso StrContainsAny(oeminf, True, "amdkmafd.sys")) Then
								For Each child As String In regkey.GetSubKeyNames()
									If IsNullOrWhitespace(child) Then Continue For

									sourceValue = TryCast(MyRegistry.OpenSubKey(regkey, child).GetValue("Source", String.Empty), String)

									If Not IsNullOrWhitespace(sourceValue) AndAlso StrContainsAny(sourceValue, True, oeminf) Then
										Try
											Deletesubregkey(regkey, child)
										Catch ex As Exception
											Application.Log.AddException(ex)
										End Try
									End If
								Next
							End If
						End If
					End If
				End Using
			End If
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

	End Sub

	Public Sub Pnplockdownfiles(ByVal driverfiles As String())

		Dim winxp = frmMain.winxp
		Dim win8higher = frmMain.win8higher
		Dim processinfo As New ProcessStartInfo
		Dim process As New Process
		Dim donotremoveamdhdaudiobusfiles = frmMain.donotremoveamdhdaudiobusfiles

		Try
			If Not winxp Then  'this does not exist on winxp so we skip if winxp detected
				If win8higher Then
					Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles", True)
						If regkey IsNot Nothing Then
							For i As Integer = 0 To driverfiles.Length - 1
								If Not IsNullOrWhitespace(driverfiles(i)) Then
									If Not (donotremoveamdhdaudiobusfiles AndAlso driverfiles(i).ToLower.Contains("amdkmafd.sys")) Then
										For Each child As String In regkey.GetSubKeyNames()
											If IsNullOrWhitespace(child) = False Then
												If child.ToLower.Replace("/", "\").Contains("\" + driverfiles(i).ToLower) Then
													Try
														Deletesubregkey(regkey, child)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try
												End If
											End If
										Next
									End If
								End If
							Next
						End If
					End Using

				Else   'Older windows  (windows vista and 7 run here)

					Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpLockdownFiles", True)
						If regkey IsNot Nothing Then
							For i As Integer = 0 To driverfiles.Length - 1
								If Not IsNullOrWhitespace(driverfiles(i)) Then
									If Not (donotremoveamdhdaudiobusfiles AndAlso driverfiles(i).ToLower.Contains("amdkmafd")) Then
										For Each child As String In regkey.GetValueNames()
											If IsNullOrWhitespace(child) = False Then
												If child.ToLower.Contains(driverfiles(i).ToLower) Then
													Try
														Deletevalue(regkey, child)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try
												End If
											End If
										Next
									End If
								End If
							Next
						End If
					End Using
				End If
			End If

		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

	End Sub

	Public Sub Clsidleftover(ByVal clsidleftover As String())

		Dim wantedvalue As String
		Dim wantedvalue2 As String
		Dim appid As String = Nothing
		Dim typelib As String = Nothing

		Application.Log.AddMessage("Begin clsidleftover CleanUP")

		Try
			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "CLSID", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) Then Continue For
						Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "CLSID\" & child & "\InProcServer32", False)
							If subregkey IsNot Nothing Then
								wantedvalue = TryCast(subregkey.GetValue("", String.Empty), String)
								If Not IsNullOrWhitespace(wantedvalue) Then
									For i As Integer = 0 To clsidleftover.Length - 1
										If Not IsNullOrWhitespace(clsidleftover(i)) Then
											If StrContainsAny(wantedvalue, True, clsidleftover(i)) Then

												appid = TryCast(MyRegistry.OpenSubKey(regkey, child).GetValue("AppID", String.Empty), String)
												If Not IsNullOrWhitespace(appid) Then
													Try
														Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "AppID", True), appid)
													Catch exARG As ArgumentException
														'Do nothing, can happen (Not found)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try
												End If

												Using subregkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child & "\TypeLib")
													If subregkey2 IsNot Nothing Then
														typelib = TryCast(subregkey2.GetValue("", String.Empty), String)
														If Not IsNullOrWhitespace(typelib) Then
															Try
																Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "TypeLib", True), typelib)
															Catch exARG As ArgumentException
																'Do nothing, can happen (Not found)
															Catch ex As Exception
																Application.Log.AddException(ex)
															End Try
														End If
													End If
												End Using

												Using reginterface As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Interface", True)
													If reginterface IsNot Nothing Then
														For Each interfacechild As String In reginterface.GetSubKeyNames
															If IsNullOrWhitespace(interfacechild) Then Continue For
															Using reginterface2 As RegistryKey = MyRegistry.OpenSubKey(reginterface, interfacechild, False)
																If reginterface2 IsNot Nothing Then
																	If MyRegistry.OpenSubKey(reginterface2, "ProxyStubClsid32") IsNot Nothing Then
																		wantedvalue2 = TryCast(MyRegistry.OpenSubKey(reginterface2, "ProxyStubClsid32").GetValue("", String.Empty), String)
																		If Not IsNullOrWhitespace(wantedvalue2) Then
																			If StrContainsAny(wantedvalue2, True, child) Then
																				Try
																					Deletesubregkey(reginterface, interfacechild)
																				Catch ex As Exception
																					Application.Log.AddException(ex, "Interface Removal via InProcServer32")
																				End Try
																			End If
																		End If
																	End If
																End If
															End Using
														Next
													End If
												End Using

												'here I remove the mediafoundationkeys if present
												'f79eac7d-e545-4387-bdee-d647d7bde42a is the Ecnoder section. Same on all windows version.
												Try
													Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "MediaFoundation\Transforms", True), child.Substring(0, child.Length - 1).Substring(1))
												Catch exARG As ArgumentException
													'Do nothing, can happen (Not found)
												Catch ex As Exception
													Application.Log.AddException(ex)
												End Try

												Try
													Deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\MediaFoundation\Transforms", True), child.Substring(0, child.Length - 1).Substring(1))
												Catch exARG As ArgumentException
													'Do nothing, can happen (Not found)
												Catch ex As Exception
													Application.Log.AddException(ex)
												End Try

												Try
													Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "MediaFoundation\Transforms\Categories\f79eac7d-e545-4387-bdee-d647d7bde42a", True), child.Substring(0, child.Length - 1).Substring(1))
												Catch exARG As ArgumentException
													'Do nothing, can happen (Not found)
												Catch ex As Exception
													Application.Log.AddException(ex)
												End Try

												Try
													Deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\MediaFoundation\Transforms\Categories\f79eac7d-e545-4387-bdee-d647d7bde42a", True), child.Substring(0, child.Length - 1).Substring(1))
												Catch exARG As ArgumentException
													'Do nothing, can happen (Not found)
												Catch ex As Exception
													Application.Log.AddException(ex)
												End Try

												Try
													Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "MediaFoundation\Transforms\Categories\d6c02d4b-6833-45b4-971a-05a4b04bab91", True), child.Substring(0, child.Length - 1).Substring(1))
												Catch exARG As ArgumentException
													'Do nothing, can happen (Not found)
												Catch ex As Exception
													Application.Log.AddException(ex)
												End Try

												Try
													Deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\MediaFoundation\Transforms\Categories\d6c02d4b-6833-45b4-971a-05a4b04bab91", True), child.Substring(0, child.Length - 1).Substring(1))
												Catch exARG As ArgumentException
													'Do nothing, can happen (Not found)
												Catch ex As Exception
													Application.Log.AddException(ex)
												End Try

												Try
													Deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\CLSID", True), child)
												Catch exARG As ArgumentException
													'Do nothing, can happen (Not found)
												Catch ex As Exception
													Application.Log.AddException(ex)
												End Try

												Try
													Deletesubregkey(regkey, child)
												Catch ex As Exception
													Application.Log.AddException(ex)
												End Try

												Exit For

											End If
										End If
									Next
								End If
							End If
						End Using
					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		Try
			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "CLSID", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) Then Continue For
						Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "CLSID\" & child, False)
							If subregkey IsNot Nothing Then
								wantedvalue = TryCast(subregkey.GetValue("", String.Empty), String)
								If Not IsNullOrWhitespace(wantedvalue) Then
									For i As Integer = 0 To clsidleftover.Length - 1
										If Not IsNullOrWhitespace(clsidleftover(i)) Then
											If StrContainsAny(wantedvalue, True, clsidleftover(i)) Then

												appid = TryCast(MyRegistry.OpenSubKey(regkey, child).GetValue("AppID", String.Empty), String)
												If Not IsNullOrWhitespace(appid) Then
													Try
														Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "AppID", True), appid)
													Catch exARG As ArgumentException
														'Do nothing, can happen (Not found)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try
												End If

												Using subregkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child & "\TypeLib")
													If subregkey2 IsNot Nothing Then
														typelib = TryCast(subregkey2.GetValue("", String.Empty), String)
														If Not IsNullOrWhitespace(typelib) Then
															Try
																Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "TypeLib", True), typelib)
															Catch exARG As ArgumentException
																'Do nothing, can happen (Not found)
															Catch ex As Exception
																Application.Log.AddException(ex)
															End Try
														End If
													End If
												End Using

												'here I remove the mediafoundationkeys if present
												'f79eac7d-e545-4387-bdee-d647d7bde42a is the Ecnoder section. Same on all windows version.

												Try
													Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "MediaFoundation\Transforms", True), child.Substring(0, child.Length - 1).Substring(1))
												Catch exARG As ArgumentException
													'Do nothing, can happen (Not found)
												Catch ex As Exception
													Application.Log.AddException(ex)
												End Try

												Try
													Deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\MediaFoundation\Transforms", True), child.Substring(0, child.Length - 1).Substring(1))
												Catch exARG As ArgumentException
													'Do nothing, can happen (Not found)
												Catch ex As Exception
													Application.Log.AddException(ex)
												End Try

												Try
													Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "MediaFoundation\Transforms\Categories\f79eac7d-e545-4387-bdee-d647d7bde42a", True), child.Substring(0, child.Length - 1).Substring(1))
												Catch exARG As ArgumentException
													'Do nothing, can happen (Not found)
												Catch ex As Exception
													Application.Log.AddException(ex)
												End Try

												Try
													Deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\MediaFoundation\Transforms\Categories\f79eac7d-e545-4387-bdee-d647d7bde42a", True), child.Substring(0, child.Length - 1).Substring(1))
												Catch exARG As ArgumentException
													'Do nothing, can happen (Not found)
												Catch ex As Exception
													Application.Log.AddException(ex)
												End Try

												Try
													Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "MediaFoundation\Transforms\Categories\d6c02d4b-6833-45b4-971a-05a4b04bab91", True), child.Substring(0, child.Length - 1).Substring(1))
												Catch exARG As ArgumentException
													'Do nothing, can happen (Not found)
												Catch ex As Exception
													Application.Log.AddException(ex)
												End Try

												Try
													Deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\MediaFoundation\Transforms\Categories\d6c02d4b-6833-45b4-971a-05a4b04bab91", True), child.Substring(0, child.Length - 1).Substring(1))
												Catch exARG As ArgumentException
													'Do nothing, can happen (Not found)
												Catch ex As Exception
													Application.Log.AddException(ex)
												End Try

												Try
													Deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\CLSID", True), child)
												Catch exARG As ArgumentException
													'Do nothing, can happen (Not found)
												Catch ex As Exception
													Application.Log.AddException(ex)
												End Try

												Try
													Deletesubregkey(regkey, child)
												Catch ex As Exception
													Application.Log.AddException(ex)
												End Try

												Exit For

											End If
										End If
									Next
								End If
							End If
						End Using
					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try


		If IntPtr.Size = 8 Then
			Try
				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\CLSID", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) Then Continue For
							Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\CLSID\" & child & "\InProcServer32", False)
								If subregkey IsNot Nothing Then
									wantedvalue = TryCast(subregkey.GetValue("", String.Empty), String)
									If Not IsNullOrWhitespace(wantedvalue) Then
										For i As Integer = 0 To clsidleftover.Length - 1
											If Not IsNullOrWhitespace(clsidleftover(i)) Then
												If StrContainsAny(wantedvalue, True, clsidleftover(i)) Then

													appid = TryCast(MyRegistry.OpenSubKey(regkey, child).GetValue("AppID", String.Empty), String)
													If Not IsNullOrWhitespace(appid) Then
														Try
															Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\AppID", True), appid)
														Catch exARG As ArgumentException
															'Do nothing, can happen (Not found)
														Catch ex As Exception
															Application.Log.AddException(ex)
														End Try
													End If

													Using subregkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child & "\TypeLib")
														If subregkey2 IsNot Nothing Then
															typelib = TryCast(subregkey2.GetValue("", String.Empty), String)
															If Not IsNullOrWhitespace(typelib) Then
																Try
																	Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\TypeLib", True), typelib)
																Catch exARG As ArgumentException
																	'Do nothing, can happen (Not found)
																Catch ex As Exception
																	Application.Log.AddException(ex)
																End Try
															End If
														End If
													End Using

													Using reginterface As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "WOW6432Node\Interface", True)
														If reginterface IsNot Nothing Then
															For Each interfacechild As String In reginterface.GetSubKeyNames
																If IsNullOrWhitespace(interfacechild) Then Continue For
																Using reginterface2 As RegistryKey = MyRegistry.OpenSubKey(reginterface, interfacechild, False)
																	If reginterface2 IsNot Nothing Then
																		If MyRegistry.OpenSubKey(reginterface2, "ProxyStubClsid32") IsNot Nothing Then
																			wantedvalue2 = TryCast(MyRegistry.OpenSubKey(reginterface2, "ProxyStubClsid32").GetValue("", String.Empty), String)
																			If StrContainsAny(wantedvalue2, True, child) Then
																				Try
																					Deletesubregkey(reginterface, interfacechild)
																				Catch ex As Exception
																					Application.Log.AddException(ex, "Interface Removal via InProcServer32")
																				End Try
																			End If
																		End If
																	End If
																End Using
															Next
														End If
													End Using


													'here I remove the mediafoundationkeys if present
													'f79eac7d-e545-4387-bdee-d647d7bde42a is the Encoder section. Same on all windows version.
													Try
														Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\MediaFoundation\Transforms", True), child.Substring(0, child.Length - 1).Substring(1))
													Catch exARG As ArgumentException
														'Do nothing, can happen (Not found)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try

													Try
														Deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\WOW6432Node\MediaFoundation\Transforms", True), child.Substring(0, child.Length - 1).Substring(1))
													Catch exARG As ArgumentException
														'Do nothing, can happen (Not found)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try

													Try
														Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\MediaFoundation\Transforms\Categories\f79eac7d-e545-4387-bdee-d647d7bde42a", True), child.Substring(0, child.Length - 1).Substring(1))
													Catch exARG As ArgumentException
														'Do nothing, can happen (Not found)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try

													Try
														Deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\WOW6432Node\MediaFoundation\Transforms\Categories\f79eac7d-e545-4387-bdee-d647d7bde42a", True), child.Substring(0, child.Length - 1).Substring(1))
													Catch exARG As ArgumentException
														'Do nothing, can happen (Not found)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try

													Try
														Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\MediaFoundation\Transforms\Categories\d6c02d4b-6833-45b4-971a-05a4b04bab91", True), child.Substring(0, child.Length - 1).Substring(1))
													Catch exARG As ArgumentException
														'Do nothing, can happen (Not found)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try

													Try
														Deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\WOW6432Node\MediaFoundation\Transforms\Categories\d6c02d4b-6833-45b4-971a-05a4b04bab91", True), child.Substring(0, child.Length - 1).Substring(1))
													Catch exARG As ArgumentException
														'Do nothing, can happen (Not found)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try

													Try
														Deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\Wow6432Node\CLSID", True), child)
													Catch exARG As ArgumentException
														'Do nothing, can happen (Not found)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try

													Try
														Deletesubregkey(regkey, child)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try

													Exit For
												End If
											End If
										Next
									End If
								End If
							End Using
						Next
					End If
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try

			Try
				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\CLSID", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) Then Continue For
							Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\CLSID\" & child, False)
								If subregkey IsNot Nothing Then
									wantedvalue = TryCast(subregkey.GetValue("", String.Empty), String)
									If Not IsNullOrWhitespace(wantedvalue) Then
										For i As Integer = 0 To clsidleftover.Length - 1
											If Not IsNullOrWhitespace(clsidleftover(i)) Then
												If StrContainsAny(wantedvalue, True, clsidleftover(i)) Then

													appid = TryCast(MyRegistry.OpenSubKey(regkey, child).GetValue("AppID", String.Empty), String)
													If Not IsNullOrWhitespace(appid) Then
														Try
															Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\AppID", True), appid)
														Catch exARG As ArgumentException
															'Do nothing, can happen (Not found)
														Catch ex As Exception
															Application.Log.AddException(ex)
														End Try
													End If

													Using subregkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child & "\TypeLib")
														If subregkey2 IsNot Nothing Then
															typelib = TryCast(subregkey2.GetValue("", String.Empty), String)
															If Not IsNullOrWhitespace(typelib) Then
																Try
																	Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\TypeLib", True), typelib)
																Catch exARG As ArgumentException
																	'Do nothing, can happen (Not found)
																Catch ex As Exception
																	Application.Log.AddException(ex)
																End Try
															End If
														End If
													End Using


													'here I remove the mediafoundationkeys if present
													'f79eac7d-e545-4387-bdee-d647d7bde42a is the Ecnoder section. Same on all windows version.

													Try
														Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\MediaFoundation\Transforms", True), child.Substring(0, child.Length - 1).Substring(1))
													Catch exARG As ArgumentException
														'Do nothing, can happen (Not found)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try

													Try
														Deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\WOW6432Node\MediaFoundation\Transforms", True), child.Substring(0, child.Length - 1).Substring(1))
													Catch exARG As ArgumentException
														'Do nothing, can happen (Not found)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try

													Try
														Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\MediaFoundation\Transforms\Categories\f79eac7d-e545-4387-bdee-d647d7bde42a", True), child.Substring(0, child.Length - 1).Substring(1))
													Catch exARG As ArgumentException
														'Do nothing, can happen (Not found)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try

													Try
														Deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\WOW6432Node\MediaFoundation\Transforms\Categories\f79eac7d-e545-4387-bdee-d647d7bde42a", True), child.Substring(0, child.Length - 1).Substring(1))
													Catch exARG As ArgumentException
														'Do nothing, can happen (Not found)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try

													Try
														Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\MediaFoundation\Transforms\Categories\d6c02d4b-6833-45b4-971a-05a4b04bab91", True), child.Substring(0, child.Length - 1).Substring(1))
													Catch exARG As ArgumentException
														'Do nothing, can happen (Not found)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try

													Try
														Deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\WOW6432Node\MediaFoundation\Transforms\Categories\d6c02d4b-6833-45b4-971a-05a4b04bab91", True), child.Substring(0, child.Length - 1).Substring(1))
													Catch exARG As ArgumentException
														'Do nothing, can happen (Not found)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try

													Try
														Deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\Wow6432Node\CLSID", True), child)
													Catch exARG As ArgumentException
														'Do nothing, can happen (Not found)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try

													Try
														Deletesubregkey(regkey, child)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try

													Exit For
												End If
											End If
										Next
									End If
								End If
							End Using
						Next
					End If
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try
		End If

		Try
			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "CLSID", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) Then Continue For
						Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "CLSID\" & child & "\LocalServer32", False)
							If subregkey IsNot Nothing Then
								wantedvalue = TryCast(subregkey.GetValue("", String.Empty), String)
								If Not IsNullOrWhitespace(wantedvalue) Then
									For i As Integer = 0 To clsidleftover.Length - 1
										If Not IsNullOrWhitespace(clsidleftover(i)) Then
											If StrContainsAny(wantedvalue, True, clsidleftover(i)) Then

												appid = TryCast(MyRegistry.OpenSubKey(regkey, child).GetValue("AppID", String.Empty), String)
												If Not IsNullOrWhitespace(appid) Then
													Try
														Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "AppID", True), appid)
													Catch exARG As ArgumentException
														'Do nothing, can happen (Not found)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try
												End If

												Using subregkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child & "\TypeLib")
													If subregkey2 IsNot Nothing Then
														typelib = TryCast(subregkey2.GetValue("", String.Empty), String)
														If Not IsNullOrWhitespace(typelib) Then
															Try
																Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "TypeLib", True), typelib)
															Catch exARG As ArgumentException
																'Do nothing, can happen (Not found)
															Catch ex As Exception
																Application.Log.AddException(ex)
															End Try
														End If
													End If
												End Using


												'here I remove the mediafoundationkeys if present
												'f79eac7d-e545-4387-bdee-d647d7bde42a is the Ecnoder section. Same on all windows version.
												Try
													Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "MediaFoundation\Transforms", True), child.Substring(0, child.Length - 1).Substring(1))
												Catch exARG As ArgumentException
													'Do nothing, can happen (Not found)
												Catch ex As Exception
													Application.Log.AddException(ex)
												End Try

												Try
													Deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\MediaFoundation\Transforms", True), child.Substring(0, child.Length - 1).Substring(1))
												Catch exARG As ArgumentException
													'Do nothing, can happen (Not found)
												Catch ex As Exception
													Application.Log.AddException(ex)
												End Try

												Try
													Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "MediaFoundation\Transforms\Categories\f79eac7d-e545-4387-bdee-d647d7bde42a", True), child.Substring(0, child.Length - 1).Substring(1))
												Catch exARG As ArgumentException
													'Do nothing, can happen (Not found)
												Catch ex As Exception
													Application.Log.AddException(ex)
												End Try

												Try
													Deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\MediaFoundation\Transforms\Categories\f79eac7d-e545-4387-bdee-d647d7bde42a", True), child.Substring(0, child.Length - 1).Substring(1))
												Catch exARG As ArgumentException
													'Do nothing, can happen (Not found)
												Catch ex As Exception
													Application.Log.AddException(ex)
												End Try

												Try
													Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "MediaFoundation\Transforms\Categories\d6c02d4b-6833-45b4-971a-05a4b04bab91", True), child.Substring(0, child.Length - 1).Substring(1))
												Catch exARG As ArgumentException
													'Do nothing, can happen (Not found)
												Catch ex As Exception
													Application.Log.AddException(ex)
												End Try

												Try
													Deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\MediaFoundation\Transforms\Categories\d6c02d4b-6833-45b4-971a-05a4b04bab91", True), child.Substring(0, child.Length - 1).Substring(1))
												Catch exARG As ArgumentException
													'Do nothing, can happen (Not found)
												Catch ex As Exception
													Application.Log.AddException(ex)
												End Try

												Try
													Deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\CLSID", True), child)
												Catch exARG As ArgumentException
													'Do nothing, can happen (Not found)
												Catch ex As Exception
													Application.Log.AddException(ex)
												End Try

												Try
													Deletesubregkey(regkey, child)
												Catch ex As Exception
													Application.Log.AddException(ex)
												End Try

												Exit For
											End If
										End If
									Next
								End If
							End If
						End Using
					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		If IntPtr.Size = 8 Then
			Try
				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\CLSID", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) Then Continue For
							Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\CLSID\" & child & "\LocalServer32", False)
								If subregkey IsNot Nothing Then
									wantedvalue = TryCast(subregkey.GetValue("", String.Empty), String)
									If Not IsNullOrWhitespace(wantedvalue) Then
										For i As Integer = 0 To clsidleftover.Length - 1
											If Not IsNullOrWhitespace(clsidleftover(i)) Then
												If StrContainsAny(wantedvalue, True, clsidleftover(i)) Then

													appid = TryCast(MyRegistry.OpenSubKey(regkey, child).GetValue("AppID", String.Empty), String)
													If Not IsNullOrWhitespace(appid) Then
														Try
															Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\AppID", True), appid)
														Catch exARG As ArgumentException
															'Do nothing, can happen (Not found)
														Catch ex As Exception
															Application.Log.AddException(ex)
														End Try
													End If

													Using subregkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child & "\TypeLib")
														If subregkey2 IsNot Nothing Then
															typelib = TryCast(subregkey2.GetValue("", String.Empty), String)
															If Not IsNullOrWhitespace(typelib) Then
																Try
																	Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\TypeLib", True), typelib)
																Catch exARG As ArgumentException
																	'Do nothing, can happen (Not found)
																Catch ex As Exception
																	Application.Log.AddException(ex)
																End Try
															End If
														End If
													End Using


													'here I remove the mediafoundationkeys if present
													'f79eac7d-e545-4387-bdee-d647d7bde42a is the Ecnoder section. Same on all windows version.

													Try
														Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\MediaFoundation\Transforms", True), child.Substring(0, child.Length - 1).Substring(1))
													Catch exARG As ArgumentException
														'Do nothing, can happen (Not found)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try

													Try
														Deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\WOW6432Node\MediaFoundation\Transforms", True), child.Substring(0, child.Length - 1).Substring(1))
													Catch exARG As ArgumentException
														'Do nothing, can happen (Not found)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try

													Try
														Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\MediaFoundation\Transforms\Categories\f79eac7d-e545-4387-bdee-d647d7bde42a", True), child.Substring(0, child.Length - 1).Substring(1))
													Catch exARG As ArgumentException
														'Do nothing, can happen (Not found)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try

													Try
														Deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\WOW6432Node\MediaFoundation\Transforms\Categories\f79eac7d-e545-4387-bdee-d647d7bde42a", True), child.Substring(0, child.Length - 1).Substring(1))
													Catch exARG As ArgumentException
														'Do nothing, can happen (Not found)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try

													Try
														Deletesubregkey(MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\MediaFoundation\Transforms\Categories\d6c02d4b-6833-45b4-971a-05a4b04bab91", True), child.Substring(0, child.Length - 1).Substring(1))
													Catch exARG As ArgumentException
														'Do nothing, can happen (Not found)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try

													Try
														Deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\WOW6432Node\MediaFoundation\Transforms\Categories\d6c02d4b-6833-45b4-971a-05a4b04bab91", True), child.Substring(0, child.Length - 1).Substring(1))
													Catch exARG As ArgumentException
														'Do nothing, can happen (Not found)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try

													Try
														Deletesubregkey(MyRegistry.OpenSubKey(Registry.LocalMachine, "SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Setup\PnpResources\Registry\HKCR\Wow6432Node\CLSID", True), child)
													Catch exARG As ArgumentException
														'Do nothing, can happen (Not found)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try

													Try
														Deletesubregkey(regkey, child)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try

													Exit For
												End If
											End If
										Next
									End If
								End If
							End Using
						Next
					End If
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try
		End If

		Try
			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "AppID", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) Then Continue For
						For i As Integer = 0 To clsidleftover.Length - 1
							If Not IsNullOrWhitespace(clsidleftover(i)) Then
								If StrContainsAny(child, True, clsidleftover(i)) Then
									Using subregkey As RegistryKey = MyRegistry.OpenSubKey(regkey, child)
										If subregkey IsNot Nothing Then
											wantedvalue = TryCast(subregkey.GetValue("AppID", String.Empty), String)
											If Not IsNullOrWhitespace(wantedvalue) Then

												Try
													Deletesubregkey(regkey, wantedvalue)
												Catch exARG As ArgumentException
													'Do nothing, can happen (Not found)
												Catch ex As Exception
													Application.Log.AddException(ex)
												End Try

												Try
													Deletesubregkey(regkey, child)
													Exit For
												Catch ex As Exception
													Application.Log.AddException(ex)
												End Try

											End If
										End If
									End Using
								End If
							End If
						Next
					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		If IntPtr.Size = 8 Then
			Try
				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Wow6432Node\AppID", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) Then Continue For
							For i As Integer = 0 To clsidleftover.Length - 1
								If Not IsNullOrWhitespace(clsidleftover(i)) Then
									If StrContainsAny(child, True, clsidleftover(i)) Then
										Using subregkey As RegistryKey = MyRegistry.OpenSubKey(regkey, child)
											If subregkey IsNot Nothing Then
												wantedvalue = TryCast(subregkey.GetValue("AppID", String.Empty), String)
												If Not IsNullOrWhitespace(wantedvalue) Then

													Try
														Deletesubregkey(regkey, wantedvalue)
													Catch exARG As ArgumentException
														'Do nothing, can happen (Not found)
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try

													Try
														Deletesubregkey(regkey, child)
														Exit For
													Catch ex As Exception
														Application.Log.AddException(ex)
													End Try

												End If
											End If
										End Using
									End If
								End If
							Next
						Next
					End If
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try
		End If


		'clean orphan typelib.....
		Application.Log.AddMessage("Orphan cleanUp")
		Try
			Dim value As String = Nothing

			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "TypeLib", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) Then Continue For

						Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child)
							If regkey2 Is Nothing Then Continue For

							For Each child2 As String In regkey2.GetSubKeyNames()
								If IsNullOrWhitespace(child2) Then Continue For

								Using regkey3 As RegistryKey = MyRegistry.OpenSubKey(regkey2, child2)
									If regkey3 Is Nothing Then Continue For

									For Each child3 As String In regkey3.GetSubKeyNames()
										If IsNullOrWhitespace(child3) Then Continue For

										Using regkey4 As RegistryKey = MyRegistry.OpenSubKey(regkey3, child3)
											If regkey4 Is Nothing Then Continue For

											For Each child4 As String In regkey4.GetSubKeyNames()
												If IsNullOrWhitespace(child4) Then Continue For

												Using regkey5 As RegistryKey = MyRegistry.OpenSubKey(regkey4, child4)
													If regkey5 Is Nothing Then Continue For

													value = TryCast(regkey5.GetValue("", String.Empty), String)    'Can also be UInt32 btw! (Usualy abnormal from personal experience,but still should be managed in the future)

													If IsNullOrWhitespace(value) Then Continue For

													For Each clsIdle As String In clsidleftover
														If IsNullOrWhitespace(clsIdle) Then Continue For

														If StrContainsAny(value, True, clsIdle) Then
															Try
																Deletesubregkey(regkey, child)
																Application.Log.AddMessage(child + " for " + clsIdle)
																Exit For
															Catch exARG As ArgumentException
																'Do nothing, can happen (Not found)
															Catch ex As Exception
																Application.Log.AddException(ex)
															End Try
														End If
													Next
												End Using
											Next
										End Using
									Next
								End Using
							Next
						End Using
					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try
		Application.Log.AddMessage("End Orphan cleanUp")
		Application.Log.AddMessage("End clsidleftover CleanUP")
	End Sub

	Public Sub Interfaces(ByVal interfaces As String())

		Application.Log.AddMessage("Start Interface CleanUP")

		Try
			Dim wantedvalue As String
			Dim typelib As String = Nothing
			Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Interface", True)
				If regkey IsNot Nothing Then
					For Each child As String In regkey.GetSubKeyNames()
						If IsNullOrWhitespace(child) Then Continue For

						Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "Interface\" & child, False)

							If subregkey IsNot Nothing Then
								wantedvalue = TryCast(subregkey.GetValue("", String.Empty), String)
								If IsNullOrWhitespace(wantedvalue) Then Continue For

								For Each iface As String In interfaces
									If IsNullOrWhitespace(iface) Then Continue For

									If StrContainsAny(wantedvalue, True, iface) Then

										Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(subregkey, "Typelib", True)
											If regkey2 IsNot Nothing Then
												typelib = TryCast(regkey2.GetValue("", String.Empty), String)
												If IsNullOrWhitespace(typelib) Then Continue For

												Try
													Deletesubregkey(regkey2, typelib)
												Catch ex As Exception
												End Try
											End If
										End Using

										Try
											Deletesubregkey(regkey, child)
										Catch ex As Exception
										End Try
									End If
								Next
							End If
						End Using
					Next
				End If
			End Using
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try

		If IntPtr.Size = 8 Then

			Try
				Dim wantedvalue As String
				Dim typelib As String = Nothing
				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "WOW6432Node\Interface", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) Then Continue For

							Using subregkey As RegistryKey = MyRegistry.OpenSubKey(Registry.ClassesRoot, "WOW6432Node\Interface\" & child, False)

								If subregkey IsNot Nothing Then
									wantedvalue = TryCast(subregkey.GetValue("", String.Empty), String)
									If IsNullOrWhitespace(wantedvalue) Then Continue For

									For Each iface As String In interfaces
										If IsNullOrWhitespace(iface) Then Continue For

										If StrContainsAny(wantedvalue, True, iface) Then

											Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(subregkey, "Typelib", True)
												If regkey2 IsNot Nothing Then
													typelib = TryCast(regkey2.GetValue("", String.Empty), String)
													If IsNullOrWhitespace(typelib) Then Continue For

													Try
														Deletesubregkey(regkey2, typelib)
													Catch ex As Exception
													End Try
												End If
											End Using

											Try
												Deletesubregkey(regkey, child)
											Catch ex As Exception
											End Try
										End If
									Next
								End If
							End Using
						Next
					End If
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try

		End If

		Application.Log.AddMessage("END Interface CleanUP")
	End Sub

	Public Sub Folderscleanup(ByVal driverfiles As String())

		Dim winxp = frmMain.winxp
		Dim donotremoveamdhdaudiobusfiles = frmMain.donotremoveamdhdaudiobusfiles
		Dim Thread1Finished = False
		Dim Thread2Finished = False
		Dim Thread3Finished = True
		Dim Thread4Finished = False
		Dim Thread5Finished = True
		Dim Thread7Finished = False
		Dim Thread8Finished = True



		Dim thread1 As Thread = New Thread(Sub() Threaddata1(Thread1Finished, Application.Paths.System32, driverfiles, donotremoveamdhdaudiobusfiles))
		thread1.Start()

		Dim thread2 As Thread = New Thread(Sub() Threaddata1(Thread2Finished, Application.Paths.System32 & "drivers\", driverfiles, donotremoveamdhdaudiobusfiles))
		thread2.Start()


		If winxp Then
			Thread3Finished = False
			Dim thread3 As Thread = New Thread(Sub() Threaddata1(Thread3Finished, Application.Paths.System32 & "drivers\dllcache\", driverfiles, donotremoveamdhdaudiobusfiles))
			thread3.Start()
		End If

		Dim thread4 As Thread = New Thread(Sub() Threaddata1(Thread4Finished, Application.Paths.WinDir, driverfiles, donotremoveamdhdaudiobusfiles))
		thread4.Start()

		If IntPtr.Size = 8 Then
			Thread8Finished = False
			Thread5Finished = False

			Dim thread8 As Thread = New Thread(Sub() Threaddata1(Thread8Finished, Application.Paths.SysWOW64, driverfiles, donotremoveamdhdaudiobusfiles))
			thread8.Start()

			Dim thread5 As Thread = New Thread(Sub() Threaddata1(Thread5Finished, Application.Paths.SysWOW64 & "Drivers\", driverfiles, donotremoveamdhdaudiobusfiles))
			thread5.Start()

		End If

		Dim thread7 As Thread = New Thread(Sub() Threaddata1(Thread7Finished, Application.Paths.WinDir & "Prefetch\", driverfiles, donotremoveamdhdaudiobusfiles))
		thread7.Start()

		While Thread1Finished <> True Or Thread2Finished <> True Or Thread3Finished <> True Or Thread4Finished <> True Or Thread5Finished <> True Or Thread7Finished <> True Or Thread8Finished <> True
			Thread.Sleep(500)
		End While

	End Sub

	Private Sub Threaddata1(ByRef ThreadFinished As Boolean, ByVal filepath As String, ByVal driverfiles As String(), ByVal donotremoveamdhdaudiobusfiles As Boolean)
		If Not Security.Principal.WindowsIdentity.GetCurrent().IsSystem Then
			ImpersonateLoggedOnUser.Taketoken()
			ACL.AddPriviliges(ACL.SE.SECURITY_NAME, ACL.SE.BACKUP_NAME, ACL.SE.RESTORE_NAME, ACL.SE.TAKE_OWNERSHIP_NAME, ACL.SE.TCB_NAME, ACL.SE.CREATE_TOKEN_NAME)
		End If

		ThreadFinished = False
		If filepath IsNot Nothing Then
			If FileIO.ExistsDir(filepath) Then
				For Each child As String In FileIO.GetFiles(filepath)
					If IsNullOrWhitespace(child) Then Continue For
					If StrContainsAny(child, True, driverfiles) Then
						Try
							Delete(child)
						Catch ex As Exception
							Application.Log.AddException(ex)
						End Try
					End If
				Next
			End If
		End If
		ThreadFinished = True
	End Sub

	Public Sub TestDelete(ByVal folder As String, config As ThreadSettings)
		' UpdateTextMethod(UpdateTextMethodmessagefn("18"))
		'Application.Log.AddMessage("Deleting some specials folders, it could take some times...")
		'ensure that this folder can be accessed with current user ac.
		If Not Directory.Exists(folder) Then
			Exit Sub
		End If

		'Get an object repesenting the directory path below
		Dim di As New DirectoryInfo(folder)

		'Traverse all of the child directors in the root; get to the lowest child
		'and delete all files, working our way back up to the top.  All files
		'must be deleted in the directory, before the directory itself can be deleted.
		'also if there is hidden / readonly / system attribute..  change those attribute.
		Try


			For Each diChild As DirectoryInfo In di.GetDirectories()
				diChild.Attributes = diChild.Attributes And Not IO.FileAttributes.ReadOnly
				diChild.Attributes = diChild.Attributes And Not IO.FileAttributes.Hidden
				diChild.Attributes = diChild.Attributes And Not IO.FileAttributes.System
				If Not (((Not config.RemovePhysX) AndAlso diChild.ToString.ToLower.Contains("physx"))) AndAlso Not diChild.ToString.ToLower.Contains("nvidia demos") Then

					Try
						TraverseDirectory(diChild, config)
					Catch ex As Exception
						Application.Log.AddException(ex)
					End Try
				End If
			Next
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try
		'Finally, clean all of the files directly in the root directory
		CleanAllFilesInDirectory(di)

		'The containing directory can only be deleted if the directory
		'is now completely empty and all files previously within
		'were deleted.
		Try
			If di.GetFiles().Length = 0 And Directory.GetDirectories(folder).Length = 0 Then
				di.Delete()
				Application.Log.AddMessage(di.ToString + " - " + "Folder removed via testdelete sub")
			End If
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try
		RemoveSharedDlls(folder)
	End Sub
	Private Sub TraverseDirectory(ByVal di As DirectoryInfo, ByVal config As ThreadSettings)

		'If the current directory has more child directories, then continure
		'to traverse down until we are at the lowest level and remove
		'there hidden / readonly / system attribute..  At that point all of the
		'files will be deleted.
		For Each diChild As DirectoryInfo In di.GetDirectories()
			diChild.Attributes = diChild.Attributes And Not IO.FileAttributes.ReadOnly
			diChild.Attributes = diChild.Attributes And Not IO.FileAttributes.Hidden
			diChild.Attributes = diChild.Attributes And Not IO.FileAttributes.System
			If Not (((Not config.RemovePhysX) AndAlso diChild.ToString.ToLower.Contains("physx"))) AndAlso Not diChild.ToString.ToLower.Contains("nvidia demos") Then

				Try
					TraverseDirectory(diChild, config)
				Catch ex As Exception
					Application.Log.AddException(ex)
				End Try
			End If
		Next

		'Now that we have no more child directories to traverse, delete all of the files
		'in the current directory, and then delete the directory itself.
		CleanAllFilesInDirectory(di)


		'The containing directory can only be deleted if the directory
		'is now completely empty and all files previously within
		'were deleted.
		If di.GetFiles().Length = 0 Then
			Try
				di.Delete()
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try
		End If

	End Sub

	''' Iterates through all files in the directory passed into
	''' method and deletes them.
	''' It may be necessary to wrap this call in impersonation or ensure parent directory
	''' permissions prior, because delete permissions are not guaranteed.

	Private Sub CleanAllFilesInDirectory(ByVal DirectoryToClean As DirectoryInfo)

		Try
			For Each fi As FileInfo In DirectoryToClean.GetFiles()
				'The following code is NOT required, but shows how some logic can be wrapped
				'around the deletion of files.  For example, only delete files with
				'a creation date older than 1 hour from the current time.  If you
				'always want to delete all of the files regardless, just remove
				'the next 'If' statement.

				'Read only files can not be deleted, so mark the attribute as 'IsReadOnly = False'

				Try
					fi.IsReadOnly = False
				Catch ex As Exception
				End Try

				Try
					fi.Delete()
				Catch ex As Exception
				End Try
				'On a rare occasion, files being deleted might be slower than program execution, and upon returning
				'from this call, attempting to delete the directory will throw an exception stating it is not yet
				'empty, even though a fraction of a second later it actually is.  Therefore the 'Optional' code below
				'can stall the process just long enough to ensure the file is deleted before proceeding. The value
				'can be adjusted as needed from testing and running the process repeatedly.
				'System.Threading.Thread.sleep(10)  '50 millisecond stall (0.025 Seconds)

			Next
		Catch ex As Exception
			Application.Log.AddException(ex)
		End Try
	End Sub

	Private Sub Delete(ByVal filename As String)
		FileIO.Delete(filename)
		RemoveSharedDlls(filename)
	End Sub

	Public Sub Cleandriverstore(ByVal config As ThreadSettings)
		Dim catalog As String = ""
		Dim CurrentProvider As String() = Nothing
		UpdateTextMethod("Executing Driver Store cleanUP(finding OEM step)...")
		Application.Log.AddMessage("Executing Driver Store cleanUP(Find OEM)...")
		'Check the driver from the driver store  ( oemxx.inf)

		UpdateTextMethod(UpdateTextTranslated(0))

		Select Case config.SelectedType
			Case CleanType.GPU
				Select Case config.SelectedGPU
					Case GPUVendor.Nvidia
						CurrentProvider = {"NVIDIA"}
					Case GPUVendor.AMD
						CurrentProvider = {"Advanced Micro Devices", "atitech", "advancedmicrodevices", "ati tech", "amd"}
					Case GPUVendor.Intel
						CurrentProvider = {"Intel"}
					Case GPUVendor.None
						CurrentProvider = {"None"}
				End Select
			Case CleanType.Audio
				Select Case config.SelectedAUDIO
					Case AudioVendor.Realtek
						CurrentProvider = {"Realtek"}
					Case AudioVendor.SoundBlaster
						CurrentProvider = {"Creative"} 'Not verified.
					Case AudioVendor.None
						CurrentProvider = {"None"}
				End Select
			Case CleanType.None
				CurrentProvider = {"None"}
				Application.Log.AddWarningMessage("CleanType is none, it is unexpected")
		End Select

		For Each oem As Inf In GetOemInfList(Application.Paths.WinDir & "inf\")
			If Not oem.IsValid Then
				Continue For
			End If

			If StrContainsAny(oem.Provider, True, CurrentProvider) Then
				'before removing the oem we try to get the original inf name (win8+)
				If frmMain.win8higher Then
					Try
						catalog = MyRegistry.OpenSubKey(Registry.LocalMachine, "DRIVERS\DriverDatabase\DriverInfFiles\" & oem.FileName).GetValue("Active").ToString
						catalog = catalog.Substring(0, catalog.IndexOf("inf_") + 3)
					Catch ex As Exception
						catalog = ""
					End Try
				End If
				If StrContainsAny(oem.Class, True, "display") Or StrContainsAny(oem.Class, True, "media") Or StrContainsAny(oem.Class, True, "extension") Or StrContainsAny(oem.Class, True, "softwarecomponent") Then
					SetupAPI.RemoveInf(oem, True)
				Else
					If Not StrContainsAny(oem.Class, True, "HDC") Then 'we dont want to ever remove an HDC class device or info.
						SetupAPI.RemoveInf(oem, False)
					End If
				End If
			End If
			'check if the oem was removed to process to the pnplockdownfile if necessary
			If frmMain.win8higher AndAlso (Not FileIO.ExistsFile(oem.FileName)) AndAlso (Not IsNullOrWhitespace(catalog)) Then
				PrePnplockdownfiles(catalog)
			End If
		Next

		UpdateTextMethod("Driver Store cleanUP complete.")

		Application.Log.AddMessage("Driver Store CleanUP Complete.")

	End Sub

	Public Sub Fixregistrydriverstore(ByVal config As ThreadSettings)
		Dim win8higher As Boolean = frmMain.win8higher
		Dim donotremoveamdhdaudiobusfiles As Boolean = frmMain.donotremoveamdhdaudiobusfiles
		'Windows 8 + only
		'This should fix driver installation problem reporting that a file is not found.
		'It is usually caused by Windows somehow losing track of the driver store , This intend to help it a bit.
		If win8higher Then
			Dim FilePath As String = Nothing
			Application.Log.AddMessage("Fixing registry driverstore if necessary")
			Try

				Dim infslist As String = ""
				For Each infs As String In My.Computer.FileSystem.GetFiles(Environment.GetEnvironmentVariable("windir") & "\inf", Microsoft.VisualBasic.FileIO.SearchOption.SearchTopLevelOnly, "oem*.inf")
					If Not IsNullOrWhitespace(infs) Then
						infslist = infslist + infs
					End If
				Next
				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "DRIVERS\DriverDatabase\DriverInfFiles", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) Then Continue For

							If child.ToLower.StartsWith("oem") AndAlso child.ToLower.EndsWith(".inf") Then
								If Not StrContainsAny(infslist, True, child) Then
									If Not IsNullOrWhitespace(MyRegistry.OpenSubKey(regkey, child).GetValue("", String.Empty).ToString) Then
										Try
											Deletesubregkey(Registry.LocalMachine, "DRIVERS\DriverDatabase\DriverPackages\" & MyRegistry.OpenSubKey(regkey, child).GetValue("", String.Empty).ToString)
										Catch ex As Exception
											Application.Log.AddException(ex)
										End Try
									End If
									Try
										Deletesubregkey(regkey, child)
									Catch ex As Exception
										Application.Log.AddException(ex)
									End Try
								End If
							End If
						Next
					End If
				End Using

				Using regkey As RegistryKey = MyRegistry.OpenSubKey(Registry.LocalMachine, "DRIVERS\DriverDatabase\DriverPackages", True)
					If regkey IsNot Nothing Then
						For Each child As String In regkey.GetSubKeyNames()
							If IsNullOrWhitespace(child) Then Continue For

							Using regkey2 As RegistryKey = MyRegistry.OpenSubKey(regkey, child)
								If regkey2 IsNot Nothing Then
									If (Not IsNullOrWhitespace(regkey2.GetValue("", String.Empty).ToString)) AndAlso
								 regkey2.GetValue("", String.Empty).ToString.ToLower.StartsWith("oem") AndAlso
								 regkey2.GetValue("", String.Empty).ToString.ToLower.EndsWith(".inf") AndAlso
								 (Not StrContainsAny(infslist, True, regkey2.GetValue("", String.Empty).ToString)) Then
										Try
											Deletesubregkey(regkey, child)
										Catch ex As Exception
											Application.Log.AddException(ex)
										End Try
									End If
								End If
							End Using
						Next
					End If
				End Using
			Catch ex As Exception
				Application.Log.AddException(ex)
			End Try

			'Cleaning of possible left-overs %windir%\system32\driverstore\filerepository
			Select Case config.SelectedGPU
				Case GPUVendor.AMD
					FilePath = System.Environment.SystemDirectory & "\DriverStore\FileRepository"
					If IsNullOrWhitespace(FilePath) = False Then
						For Each child As String In FileIO.GetDirectories(FilePath)
							If IsNullOrWhitespace(child) = False Then
								Dim dirinfo As New System.IO.DirectoryInfo(child)
								If dirinfo.Name.ToLower.StartsWith("c030") Or
								 StrContainsAny(dirinfo.Name, True, "atihdwt6.inf") Or
								 (Not donotremoveamdhdaudiobusfiles AndAlso StrContainsAny(dirinfo.Name, True, "amdkmafd.inf")) Then
									Try
										Delete(child)
									Catch ex As Exception
									End Try
								End If
							End If
						Next
					End If
				Case GPUVendor.Nvidia
					FilePath = System.Environment.SystemDirectory & "\DriverStore\FileRepository"
					If IsNullOrWhitespace(FilePath) = False Then
						For Each child As String In FileIO.GetDirectories(FilePath)
							If IsNullOrWhitespace(child) = False Then
								Dim dirinfo As New System.IO.DirectoryInfo(child)
								If StrContainsAny(dirinfo.Name, True, "nvstusb.inf", "nvhda.inf", "nv_dispi.inf") Then
									Try
										Delete(child)
									Catch ex As Exception
									End Try
								End If
								If config.RemoveGFE Then
									If StrContainsAny(dirinfo.Name, True, "nvvad.inf", "nvswcfilter.inf") Then
										Try
											Delete(child)
										Catch ex As Exception
										End Try
									End If
								End If
							End If
						Next
					End If
			End Select
		End If
	End Sub


	Private Sub UpdateTextMethod(ByVal strMessage As String)
		frmMain.UpdateTextMethod(strMessage)
	End Sub

	Private Function UpdateTextTranslated(ByVal number As Integer) As String
		Return frmMain.UpdateTextTranslated(number)
	End Function

End Class
