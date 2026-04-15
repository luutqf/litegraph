export type CodeLanguage = 'curl' | 'javascript' | 'csharp';

type SnippetInput = {
  method: string;
  url: string;
  headers: Record<string, string>;
  body?: string;
};

const formatCurl = ({ method, url, headers, body }: SnippetInput): string => {
  const parts: string[] = [`curl -X ${method} \\`];
  parts.push(`  "${url}" \\`);
  for (const [k, v] of Object.entries(headers)) {
    parts.push(`  -H "${k}: ${v}" \\`);
  }
  if (body) {
    const escaped = body.replace(/\\/g, '\\\\').replace(/"/g, '\\"');
    parts.push(`  -d "${escaped}"`);
  } else {
    const last = parts[parts.length - 1];
    parts[parts.length - 1] = last.endsWith(' \\') ? last.slice(0, -2) : last;
  }
  return parts.join('\n');
};

const formatJavaScript = ({ method, url, headers, body }: SnippetInput): string => {
  const init: Record<string, unknown> = {
    method,
    headers,
  };
  if (body) init.body = body;
  return `const response = await fetch(${JSON.stringify(url)}, ${JSON.stringify(init, null, 2)});
const data = await response.json();
console.log(data);`;
};

const formatCSharp = ({ method, url, headers, body }: SnippetInput): string => {
  const lines: string[] = [];
  lines.push('using var client = new HttpClient();');
  lines.push(`var request = new HttpRequestMessage(HttpMethod.${method.charAt(0) + method.slice(1).toLowerCase()}, ${JSON.stringify(url)});`);
  for (const [k, v] of Object.entries(headers)) {
    if (k.toLowerCase() === 'content-type') continue;
    lines.push(`request.Headers.TryAddWithoutValidation(${JSON.stringify(k)}, ${JSON.stringify(v)});`);
  }
  if (body) {
    const ct = headers['Content-Type'] || headers['content-type'] || 'application/json';
    lines.push(`request.Content = new StringContent(${JSON.stringify(body)}, System.Text.Encoding.UTF8, ${JSON.stringify(ct)});`);
  }
  lines.push('var response = await client.SendAsync(request);');
  lines.push('var body = await response.Content.ReadAsStringAsync();');
  lines.push('Console.WriteLine(body);');
  return lines.join('\n');
};

export const generateSnippet = (lang: CodeLanguage, input: SnippetInput): string => {
  switch (lang) {
    case 'curl':
      return formatCurl(input);
    case 'javascript':
      return formatJavaScript(input);
    case 'csharp':
      return formatCSharp(input);
    default:
      return '';
  }
};
