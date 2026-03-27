from ..configuration import get_client


class Admin:
    """Administrative operations for LiteGraph server."""

    @classmethod
    def list_backups(cls):
        """List all available backups."""
        client = get_client()
        return client.request("GET", "v1.0/backups")

    @classmethod
    def create_backup(cls):
        """Create a new database backup."""
        client = get_client()
        return client.request("POST", "v1.0/backups")

    @classmethod
    def read_backup(cls, backup_filename: str):
        """Read a specific backup file."""
        client = get_client()
        return client.request("GET", f"v1.0/backups/{backup_filename}")

    @classmethod
    def backup_exists(cls, backup_filename: str) -> bool:
        """Check if a backup file exists."""
        client = get_client()
        try:
            client.request("HEAD", f"v1.0/backups/{backup_filename}")
            return True
        except Exception:
            return False

    @classmethod
    def delete_backup(cls, backup_filename: str):
        """Delete a backup file."""
        client = get_client()
        return client.request("DELETE", f"v1.0/backups/{backup_filename}")

    @classmethod
    def flush(cls):
        """Flush in-memory database to disk."""
        client = get_client()
        return client.request("POST", "v1.0/flush")
