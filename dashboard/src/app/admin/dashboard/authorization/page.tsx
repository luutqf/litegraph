import AuthorizationPage from '@/page/authorization/AuthorizationPage';
import { Metadata } from 'next';
import React from 'react';

export const metadata: Metadata = {
  title: 'LiteGraph | Authorization',
  description: 'LiteGraph',
};

const Authorization = () => {
  return <AuthorizationPage />;
};

export default Authorization;
