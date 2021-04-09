using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace rdpcontroller
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
        }

        private void FormStaList_Load(object sender, EventArgs e)
        {
            webBrowser.ObjectForScripting = new HtmlPage(this, webBrowser);
            webBrowser.DocumentCompleted += webBrowser_DocumentCompleted;
            webBrowser.Navigate("about:blank");
        }

        void webBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            webBrowser.Document.Write(@"<html>
<head>
<style>
html,body{background:#eee;overflow:auto;width:100%;height:100%;padding:0px;margin:0px;}
table{border-collapse:collapse;}
thead{background:#aaa;}
tbody{background:#ddd;}
</style>
<script>
function onsave()
{
    var tblhtml = document.getElementById('tbl').innerHTML;
    window.external.SaveData(tblhtml);
}
function onconnect(e)
{
    var tr = e.srcElement.parentNode.parentNode;
    var code = tr.childNodes[1].innerText;
    var pwd = tr.childNodes[2].innerText;
    window.external.Connect(code,pwd);
}
function onaddline(e)
{
    var tb = document.getElementsByTagName('tbody')[0];
    var newdiv = document.createElement('div');
    newdiv.innerHTML = '<table><tbody><tr><td><input/></td><td><input/></td><td><input/></td><td><textarea></textarea></td><td><button onclick=""onconnect(event)"">连接</button><button onclick=""oneditline(event)"">保存</button><button onclick=""ondelline(event)"">删除</button></td></tr></tbody></table>';
    tb.appendChild(newdiv.firstChild.firstChild.firstChild);
}
function oneditline(e)
{
    var tr = e.srcElement.parentNode.parentNode;
    if(e.srcElement.innerText == '修改')
    {
        e.srcElement.innerText = '保存';
        tr.childNodes[0].innerHTML = '<input value=""'+tr.childNodes[0].innerText+'""/>';
        tr.childNodes[1].innerHTML = '<input value=""'+tr.childNodes[1].innerText+'""/>';
        tr.childNodes[2].innerHTML = '<input value=""'+tr.childNodes[2].innerText+'""/>';
        tr.childNodes[3].innerHTML = '<textarea>'+tr.childNodes[3].innerText+'</textarea>';
    }else{
        e.srcElement.innerText = '修改';
        tr.childNodes[0].innerText = tr.childNodes[0].firstChild.value;
        tr.childNodes[1].innerHTML = tr.childNodes[1].firstChild.value;
        tr.childNodes[2].innerHTML = tr.childNodes[2].firstChild.value;
        tr.childNodes[3].innerHTML = tr.childNodes[3].firstChild.value;
        onsave();
    }
}
function ondelline(e)
{
    if(!confirm('确定要删除吗?'))
    {
        return;
    }
    var tr = e.srcElement.parentNode.parentNode;
    tr.parentNode.removeChild(tr);
    onsave();
}
</script>
</head>
<body onload='onloaddata()'>
<button onclick='onaddline(event)'>添加</button><button onclick='onsave(event)'>存档</button>
<div id='tbl'>
<table border='1' width='100%'>
<thead><th>名称</th><th>远程ID</th><th>远程密码</th><th>备注</th><th>操作</th></thead>
<tbody>
<tr><td><input/></td><td><input/></td><td><input/></td><td><textarea></textarea></td><td><button onclick=""onconnect(event)"">连接</button><button onclick=""oneditline(event)"">保存</button><button onclick=""ondelline(event)"">删除</button></td></tr>
</tbody></table></div>
</body>
<script>
var data = window.external.LoadData();
if(data != null && data != ''){
    document.getElementById('tbl').innerHTML=data;
}
</script></html>");
        }
    }

    [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Demand, Name = "FullTrust")]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public class HtmlPage
    {
        FormMain m_main;
        System.Windows.Forms.WebBrowser webBrowser;

        public HtmlPage(FormMain main, System.Windows.Forms.WebBrowser webBrowser)
        {
            m_main = main;
            this.webBrowser = webBrowser;
        }

        public void Connect(string code,string pwd)
        {
            FormController controller = new FormController(code,pwd);
            controller.Show();
        }

        public string LoadData()
        {
            try
            {
                string curpath = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                string datafile = curpath + "\\data.dat";
                return File.ReadAllText(datafile);
            }
            catch
            {
            }
            return "";
        }

        public void SaveData(string data)
        {
            try
            {
                string curpath = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                string datafile = curpath + "\\data.dat";
                File.WriteAllText(datafile, data);
            }
            catch
            {
            }
        }
    }
}
