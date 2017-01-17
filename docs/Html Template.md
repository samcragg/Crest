# HTML Template

Although a typical micro-service will return machine readable data (such as
JSON, XML etc.), during development (and deployment) you can use a browser to
query the service. To render HTML, the framework uses a class implementing the
`IHtmlTemplateProvider` interface to provide the structure of the page, which by
default includes a little Crest branding. This can be easily customised by
overriding the relevant method in the `Bootstrapper`.

## Creating a custom template

First create a class that implements the `IHtmlTemplateProvider` interface. This
is a simple interface that provides three properties. The `Template` property is
used to return the entire HTML page, the `ContentLocation` specifies the index
of where to insert the generated HTML content and finally the `HintText`
provides optional HTML that will appear at the top of HTML generated for models
that gives a hint to developers as to why they're receiving HTML content back
instead of JSON etc. because the `Accepts` header was missing.

Here's a basic example:

```C#
internal sealed class HtmlTemplateProvider : IHtmlTemplateProvider
{
    public int ContentLocation
    {
        // This could be hard coded instead...
        get { return this.Template.IndexOf("</body>"); }
    }

    public string HintText
    {
        get { return "<p>Did you forget the <code>Accept</code> header?</p>"; }
    }

    public string Template
    {
        get
        {
            return @"<!doctype html>
<html>
<head>
<meta charset=utf-8>
<title>My Template</title>
</head>
<body>
</body>
</html>";
            }
        }
    }
```

If a single class exists implementing the interface then it will automatically
get picked up by Crest so that's it! However, if you have multiple
implementations then you will need to resolve which one to use by customising
the service locator.
