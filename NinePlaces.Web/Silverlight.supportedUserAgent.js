	//v4.0.50401.0
	if(!window.Silverlight)
	window.Silverlight={};
	Silverlight.supportedUserAgent=function(g,f)
	{
		try
		{
			var b=null;
			if(f)
				b=f;
			else 
				b=window.navigator.userAgent;
				
			var a={OS:"Unsupported",Browser:"Unsupported"};
			
			if(b.indexOf("Windows NT")>=0||b.indexOf("Mozilla/4.0 (compatible; MSIE 6.0)")>=0)
				a.OS="Windows";
			else if(b.indexOf("PPC Mac OS X")>=0)
				a.OS="MacPPC";
			else if(b.indexOf("Intel Mac OS X")>=0)
				a.OS="MacIntel";
			else if(b.indexOf("Linux")>=0)
				a.OS="Linux";
				
			if(a.OS!="Unsupported")
				if(b.indexOf("MSIE")>=0)
				{
					if(navigator.userAgent.indexOf("Win64")==-1)
						if(parseInt(b.split("MSIE")[1])>=6)
							a.Browser="MSIE"
				}
				else if(b.indexOf("Firefox")>=0)
				{
					var e=b.split("Firefox/")[1].split("."),h=parseInt(e[0]);
					if(h>=2)
						a.Browser="Firefox";
					else
					{
						var i=parseInt(e[1]);
						if(h==1&&i>=5)
							a.Browser="Firefox"
					}
				}
				else if(b.indexOf("Chrome")>=0)
					a.Browser="Chrome";
				else if(b.indexOf("Safari")>=0)
					a.Browser="Safari";
			
			var d=parseInt(g),c=!(a.OS=="Unsupported"||a.Browser=="Unsupported"||a.OS=="Windows"&&a.Browser=="Safari"||a.OS.indexOf("Mac")>=0&&a.Browser=="MSIE"||a.OS.indexOf("Mac")>=0&&a.Browser=="Chrome");
			
			if(a.OS.indexOf("Windows")>=0&&a.Browser=="Chrome"&&d<4)
				return false;
			if(a.OS=="MacPPC"&&d>1)
				return c&&a.OS!="MacPPC";
			if(a.OS=="Linux"&&d>2)
				return c&&a.OS!="Linux";
			if(g=="1.0")
				return c&&b.indexOf("Windows NT 5.0")<0;
			else 
				return c
		}
		catch(j)
		{
			return false
		}
	}