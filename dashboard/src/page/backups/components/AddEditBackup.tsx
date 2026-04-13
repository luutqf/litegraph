'use client';

import { useEffect, useState } from 'react';
import { Form } from 'antd';
import { globalToastId } from '@/constants/config';
import LitegraphFormItem from '@/components/base/form/FormItem';
import LitegraphModal from '@/components/base/modal/Modal';

import LitegraphInput from '@/components/base/input/Input';
import { toast } from 'react-hot-toast';
import { BackupMetaData, BackupMetaDataCreateRequest } from 'litegraphdb/dist/types/types';
import { useCreateBackupMutation } from '@/lib/store/slice/slice';

interface AddEditBackupProps {
  isAddEditBackupVisible: boolean;
  setIsAddEditBackupVisible: (visible: boolean) => void;
  backup: BackupMetaData | null;
  onBackupUpdated?: () => Promise<void>;
}

const AddEditBackup = ({
  isAddEditBackupVisible,
  setIsAddEditBackupVisible,
  backup,
  onBackupUpdated,
}: AddEditBackupProps) => {
  const [form] = Form.useForm<BackupMetaDataCreateRequest>();
  const [formValid, setFormValid] = useState(false);
  const [createBackupService, { isLoading: createBackupLoading }] = useCreateBackupMutation();
  const [formValues, setFormValues] = useState({});

  useEffect(() => {
    form
      .validateFields({ validateOnly: true })
      .then(() => setFormValid(true))
      .catch(() => setFormValid(false));
  }, [formValues, form]);

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      const backupData: BackupMetaDataCreateRequest = {
        Filename: values.Filename,
      };
      const success = await createBackupService(backupData);

      if (success) {
        setIsAddEditBackupVisible(false);
        form.resetFields();
        onBackupUpdated && onBackupUpdated();
        toast.success('Backup created successfully', { id: globalToastId });
      } else {
        toast.error('Failed to create backup', { id: globalToastId });
      }
    } catch (error: unknown) {
      console.error('Failed to submit:', error);
      const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
      toast.error(`Failed to create backup: ${errorMessage}`, { id: globalToastId });
    }
  };

  return (
    <LitegraphModal
      title={backup ? 'Edit Backup' : 'Add Backup'}
      open={isAddEditBackupVisible}
      onOk={handleSubmit}
      onCancel={() => {
        setIsAddEditBackupVisible(false);
        form.resetFields();
      }}
      confirmLoading={createBackupLoading}
      okButtonProps={{ disabled: !formValid }}
    >
      <Form
        form={form}
        layout="vertical"
        onValuesChange={(_, allValues) => setFormValues(allValues)}
      >
        <LitegraphFormItem
          label="Filename"
          name="Filename"
          tooltip="Name for the backup file"
          rules={[{ required: true, message: 'Please enter a filename' }]}
        >
          <LitegraphInput placeholder="Enter filename" data-testid="filename-input" />
        </LitegraphFormItem>
      </Form>
    </LitegraphModal>
  );
};

export default AddEditBackup;
