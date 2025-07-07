import axios from 'axios'

const AZURE_FUNCTION_BASE_URL = 'https://your-function-app.azurewebsites.net/api'

export interface SavedQuery {
  id: number
  userId: string
  applicationId: number
  eventId: number
  label: string
  nqlQuestion: string
  generatedSQL: string
  hasData: boolean
  tokensUsed: number
  thumbsUp: boolean | null
  createdAt: string
  lastUsed: string | null
  feedbackTimestamp: string | null
  useCount: number
  requestId: string
  eventType: string
  metadata: string | null
  isActive: boolean
  isSampleQuery: boolean
  category: string | null
}

export interface SaveQueryRequest {
  userId: string
  applicationId: number
  eventId: number
  label: string
  nqlQuestion: string
  generatedSQL: string
  hasData: boolean
  tokensUsed: number
  thumbsUp?: boolean | null
  requestId?: string
  eventType?: string
  metadata?: string
}

export interface QueryStats {
  totalQueries: number
  successfulQueries: number
  queriesWithPositiveFeedback: number
  queriesWithNegativeFeedback: number
  totalTokensUsed: number
  totalUseCount: number
  lastQueryDate: string | null
  mostUsedQuery: string | null
}

class SavedQueriesService {
  private getHeaders() {
    return {
      'Content-Type': 'application/json',
      'x-functions-key': process.env.AZURE_FUNCTION_KEY || 'your-function-key'
    }
  }

  async saveQuery(request: SaveQueryRequest): Promise<SavedQuery> {
    try {
      const response = await axios.post(
        `${AZURE_FUNCTION_BASE_URL}/saved-queries`,
        request,
        { headers: this.getHeaders() }
      )
      
      if (response.data.success) {
        return response.data.data
      } else {
        throw new Error('Failed to save query')
      }
    } catch (error) {
      console.error('Error saving query:', error)
      throw error
    }
  }

  async getUserQueries(
    userId: string,
    options: {
      applicationId?: number
      hasData?: boolean
      thumbsUp?: boolean
      searchTerm?: string
      pageNumber?: number
      pageSize?: number
    } = {}
  ): Promise<{
    data: SavedQuery[]
    totalCount: number
    pageNumber: number
    pageSize: number
    totalPages: number
  }> {
    try {
      const params = new URLSearchParams()
      
      if (options.applicationId) params.append('applicationId', options.applicationId.toString())
      if (options.hasData !== undefined) params.append('hasData', options.hasData.toString())
      if (options.thumbsUp !== undefined) params.append('thumbsUp', options.thumbsUp.toString())
      if (options.searchTerm) params.append('searchTerm', options.searchTerm)
      if (options.pageNumber) params.append('pageNumber', options.pageNumber.toString())
      if (options.pageSize) params.append('pageSize', options.pageSize.toString())

      const response = await axios.get(
        `${AZURE_FUNCTION_BASE_URL}/saved-queries/user/${userId}?${params.toString()}`,
        { headers: this.getHeaders() }
      )
      
      if (response.data.success) {
        return response.data
      } else {
        throw new Error('Failed to get user queries')
      }
    } catch (error) {
      console.error('Error getting user queries:', error)
      throw error
    }
  }

  async getSampleQueries(options: {
    applicationId?: number
    category?: string
    limit?: number
  } = {}): Promise<SavedQuery[]> {
    try {
      const params = new URLSearchParams()
      
      if (options.applicationId) params.append('applicationId', options.applicationId.toString())
      if (options.category) params.append('category', options.category)
      if (options.limit) params.append('limit', options.limit.toString())

      const response = await axios.get(
        `${AZURE_FUNCTION_BASE_URL}/sample-queries?${params.toString()}`,
        { headers: this.getHeaders() }
      )
      
      if (response.data.success) {
        return response.data.data
      } else {
        throw new Error('Failed to get sample queries')
      }
    } catch (error) {
      console.error('Error getting sample queries:', error)
      throw error
    }
  }

  async updateFeedback(queryId: number, thumbsUp: boolean): Promise<void> {
    try {
      const response = await axios.patch(
        `${AZURE_FUNCTION_BASE_URL}/saved-queries/${queryId}/feedback`,
        { thumbsUp },
        { headers: this.getHeaders() }
      )
      
      if (!response.data.success) {
        throw new Error('Failed to update feedback')
      }
    } catch (error) {
      console.error('Error updating feedback:', error)
      throw error
    }
  }

  async updateUsage(queryId: number): Promise<void> {
    try {
      const response = await axios.patch(
        `${AZURE_FUNCTION_BASE_URL}/saved-queries/${queryId}/usage`,
        {},
        { headers: this.getHeaders() }
      )
      
      if (!response.data.success) {
        throw new Error('Failed to update usage')
      }
    } catch (error) {
      console.error('Error updating usage:', error)
      throw error
    }
  }

  async getQueryStats(userId: string, applicationId?: number): Promise<QueryStats> {
    try {
      const params = applicationId ? `?applicationId=${applicationId}` : ''
      
      const response = await axios.get(
        `${AZURE_FUNCTION_BASE_URL}/saved-queries/stats/${userId}${params}`,
        { headers: this.getHeaders() }
      )
      
      if (response.data.success) {
        return response.data.data
      } else {
        throw new Error('Failed to get query stats')
      }
    } catch (error) {
      console.error('Error getting query stats:', error)
      throw error
    }
  }

  async deleteQuery(queryId: number): Promise<void> {
    try {
      const response = await axios.delete(
        `${AZURE_FUNCTION_BASE_URL}/saved-queries/${queryId}`,
        { headers: this.getHeaders() }
      )
      
      if (!response.data.success) {
        throw new Error('Failed to delete query')
      }
    } catch (error) {
      console.error('Error deleting query:', error)
      throw error
    }
  }
}

export const savedQueriesService = new SavedQueriesService()