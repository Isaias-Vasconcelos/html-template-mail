
# 📄 HtmlMail Template Engine (.NET)

Uma pequena biblioteca para gerar **templates HTML** com interpolação de C# diretamente no HTML, sem depender de Razor.

## ✅ Funcionalidades

- Template HTML com expressões C# inline: `@{Model.Property}` ou `@{ código C# }`
- Blocos de código C# dentro do HTML
- Suporte a Model fortemente tipado
- Inclusão de CSS externo no template
- Compilação dinâmica com Roslyn (`Microsoft.CodeAnalysis.CSharp.Scripting`)

---

## 🚀 Como usar

### 1. Instale o pacote NuGet necessário

A biblioteca depende de:

- `Microsoft.CodeAnalysis.CSharp.Scripting`
- `HtmlMail.TemplateEngine`

No seu projeto:

```bash
dotnet add package Microsoft.CodeAnalysis.CSharp.Scripting
dotnet add package HtmlMail.TemplateEngine
```

---

### 2. Estrutura esperada

Você precisa ter:

- Um arquivo `.html` como **template**
- Um arquivo `.css` para estilos
- Um objeto Model (classe C#) com os dados

---

### Exemplo de HTML Template:

**caminho:** `template.html`

```html
<html>
<head>
    <style></style>
</head>
<body>
    <h1>Olá @Model.Nome!</h1>

    <p>Sua idade: @{ Model.Idade + 1 }</p>

    @{
    if (Model.Idade >= 18)
    {
       sb.Append("<p>É Maior de idade </p>");
    } else
    {
      sb.Append("<p>Não é Maior de idade</p>");
    }
    }

    <p>Status: @Model.Status</p>
</body>
</html>
```

---

### Exemplo de CSS:

**caminho:** `style.css`

```css
body {
    font-family: Arial, sans-serif;
    color: #333;
}
```

---

### Exemplo de Model:

```csharp
public class Usuario
{
    public string Nome { get; set; } = "";
    public int Idade { get; set; }
    public string Status { get; set; } = "";
}
```

---

### Exemplo de uso no C#:

```csharp
using HtmlMail;

var usuario = new Usuario
{
    Nome = "João",
    Idade = 20,
    Status = "Ativo"
};

string caminhoHtml = "template.html";
string caminhoCss = "style.css";

string htmlGerado = await Template.GenerateHtml(caminhoHtml, caminhoCss, usuario);

Console.WriteLine(htmlGerado);
```

---

### ✅ Resultado esperado:

```html
<html>
<head>
    <style>
body {
    font-family: Arial, sans-serif;
    color: #333;
}
    </style>
</head>
<body>
    <h1>Olá João!</h1>

    <p>Sua idade: 21</p>

    <p>É Maior de idade</p>

    <p>Status: Ativo</p>
</body>
</html>
```

---

## ✅ Sintaxe suportada no HTML:

| Tipo                          | Exemplo                              | Resultado                          |
|-------------------------------|--------------------------------------|------------------------------------|
| Expressão simples             | `@Model.Nome`                        | Substitui pelo valor da propriedade |
| Expressão C# inline           | `@{ Model.Idade + 1 }`               | Avalia a expressão e insere o resultado |
| Bloco de código               | `@{ if (...) { sb.Append(...); } }` | Executa um bloco de código C# durante a renderização |

> 💡 Importante: Dentro de blocos de código, use `sb.Append()` para adicionar HTML.

---

## ⚠️ Cuidados

- O template é compilado e executado como código C#. **Cuidado com código malicioso se o template for fornecido por usuários externos.**
- Exceções de compilação são retornadas com a mensagem e o script gerado para facilitar o debug.

---

## 📌 Requisitos:

- .NET 6 ou superior
- Pacote NuGet: `Microsoft.CodeAnalysis.CSharp.Scripting`

---

## ✅ Possíveis melhorias futuras:

- Suporte a Partial Views
- Cache de scripts compilados

---

## 📃 Licença

Fique a vontade!
