# VRGIN for Unity5.6.5
Eusth氏作成[VRGIN](https://github.com/Eusth/VRGIN)で、Unity5.6.5系のアプリケーションで
レンダリングが崩れる問題があったため対応したもの。

Mr. Eusth created [VRGIN] (https://github.com/Eusth/VRGIN), in Unity 5.6.5 series applications
Corresponded because there was a problem that rendering collapsed.

## 使用方法 How to Use It
Eusth氏作成[VRGIN](https://github.com/Eusth/VRGIN)記載の使用手順を参照ください。
また、追加でVR化対象のゲームにパッチ当てが必要です。
下記を参考に実施ください。

Please refer to the use procedure described by Eusth [VRGIN] (https://github.com/Eusth/VRGIN).
In addition, it is necessary to apply patch to the game to be VRized additionally.
Please carry out referring to the following.

### VR化のためのパッチあて To the patch for VR conversion.
#### 必要なツール Need tools.
- [UABE(Unity Asset Bundle Extractor) 2.2beta2](https://github.com/DerPopo/UABE/releases)

#### パッチ当て方法 How to Patching
- ゲームインストールフォルダにある「ゲーム名\_Data/globalgamemanagers」をUABEで開く。
- Path ID列が11のTypeがBuild Settingsとなっている行を選択し、Export Dumpを行う。
- 作成されたダンプファイルを開き、22行目の0 vector enabledVRDevicesから0 int size = 0までを下記のように修正。

- Open "Game name\_Data/globalgamemanagers" in the game installation folder with UABE.
- Select the row whose Path ID column is 11 and whose Type is Build Settings, and perform Export Dump.
- Open the created dump file and fix from 0 vector enabledVRDevices on line 22 to 0 int size = 0 as shown below.

_修正前 preview fix_

0 vector enabledVRDevices  
 0 Array Array (0 items)  
  0 int size = 0  

_修正後 after fix_

0 vector enabledVRDevices  
  0 Array Array (2 items)  
   0 int size = 2  
   [0]  
    1 string data = "OpenVR"  
   [1]  
    1 string data = "None"  

\*1)0 vector buildTags の前行まで置き換える/0 Replace the previous line of vector buildTags.  
\*2)"Oculus"にすればOculusでも動くかもしれませんが、未確認です。/If it is "Oculus" it may work with Oculus, but it is unconfirmed.  

- 再度UABEでglobalgamemanagerを開き、修正したダンプファイルをインポートして更新してください。  
- Open globalgamemanager again with UABE and import and update the modified dump file.
