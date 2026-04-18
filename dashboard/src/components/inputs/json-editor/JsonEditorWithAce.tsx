'use client';

import ace from 'ace-builds/src-noconflict/ace';
import 'ace-builds/src-noconflict/ext-language_tools';
import 'ace-builds/src-noconflict/mode-json';
import 'ace-builds/src-noconflict/theme-textmate';
import { JsonEditor } from 'jsoneditor-react';

const JsonEditorWithAce = (props: any) => {
  return <JsonEditor ace={ace} theme="ace/theme/textmate" {...props} />;
};

export default JsonEditorWithAce;
